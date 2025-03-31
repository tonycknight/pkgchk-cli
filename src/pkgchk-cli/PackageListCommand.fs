namespace pkgchk

open System.ComponentModel
open System.Diagnostics.CodeAnalysis
open Spectre.Console.Cli

[<ExcludeFromCodeCoverage>]
type PackageListCommandSettings() =
    inherit PackageCommandSettings()

    [<CommandOption("-t|--transitive")>]
    [<Description("Toggle transitive package checks. true to include them, false to exclude.")>]
    [<DefaultValue(true)>]
    member val IncludeTransitives = true with get, set

[<ExcludeFromCodeCoverage>]
type PackageListCommand(nuget: Tk.Nuget.INugetClient) =
    inherit Command<PackageListCommandSettings>()

    let console = Spectre.Console.AnsiConsole.MarkupLine

    let trace traceLogging =
        if traceLogging then Console.grey >> console else ignore

    let returnError error =
        error |> Console.error |> console
        ReturnCodes.sysError

    let runProc logging proc =
        try
            proc |> Io.run logging
        finally
            proc.Dispose()

    let runRestore (settings: PackageListCommandSettings) logging =
        if settings.NoRestore then
            Choice1Of2 false
        else
            let runRestoreProcParse run proc =
                proc
                |> run
                |> (function
                | Choice2Of2 error -> Choice2Of2 error
                | _ -> Choice1Of2 true)

            settings.ProjectPath
            |> Sca.restoreArgs
            |> Io.createProcess
            |> runRestoreProcParse (runProc logging)

    let getErrors procResults =
        procResults
        |> Seq.map (function
            | Choice2Of2 x -> x
            | _ -> "")
        |> Seq.filter String.isNotEmpty
        |> Seq.distinct

    let renderTables (values: seq<Spectre.Console.Table>) =
        values |> Seq.iter Spectre.Console.AnsiConsole.Write

    let liftHits procResults =
        procResults
        |> Seq.collect (function
            | Choice1Of2 xs -> xs
            | _ -> [])
        |> List.ofSeq

    let sortHits (hits: seq<ScaHit>) =
        hits
        |> Seq.sortBy (fun h ->
            ((match h.kind with
              | ScaHitKind.Vulnerability -> 0
              | ScaHitKind.Dependency -> 1
              | ScaHitKind.VulnerabilityTransitive -> 2
              | ScaHitKind.Deprecated -> 3
              | ScaHitKind.DependencyTransitive -> 4),
             h.packageId))

    let getHits = liftHits >> sortHits >> List.ofSeq

    override _.Execute(context, settings) =
        let trace = trace settings.TraceLogging

        if settings.NoBanner |> not then
            nuget |> App.banner |> console

        match runRestore settings trace with
        | Choice2Of2 error -> error |> returnError
        | _ ->
            let results =
                (settings.ProjectPath, false, settings.IncludeTransitives, false, true)
                |> Sca.scanArgs
                |> Array.map (fun (args, parser) -> (Io.createProcess args, parser))
                |> Array.map (fun (proc, parser) ->
                    match proc |> (runProc trace) with
                    | Choice1Of2 json -> parser json
                    | Choice2Of2 x -> Choice2Of2 x)

            let errors = getErrors results

            if Seq.isEmpty errors |> not then
                errors |> String.joinLines |> returnError
            else
                trace "Analysing results..."
                let hits = getHits results
                let hitCounts = hits |> Sca.hitCountSummary |> List.ofSeq

                trace "Building display..."

                let renderables =
                    seq {
                        hits |> Console.hitsTable

                        if hitCounts |> List.isEmpty |> not then
                            hitCounts |> Console.hitSummaryTable
                    }

                renderTables renderables

                ReturnCodes.validationOk
