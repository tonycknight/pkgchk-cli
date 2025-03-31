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
       
    override _.Execute(context, settings) =
        let trace = Commands.trace settings.TraceLogging

        if settings.NoBanner |> not then
            nuget |> App.banner |> Commands.console

        match Commands.runRestore settings trace with
        | Choice2Of2 error -> error |> Commands.returnError
        | _ ->
            let results =
                (settings.ProjectPath, false, settings.IncludeTransitives, false, true)
                |> Sca.scanArgs
                |> Array.map (fun (args, parser) -> (Io.createProcess args, parser))
                |> Array.map (fun (proc, parser) ->
                    match proc |> (Commands.runProc trace) with
                    | Choice1Of2 json -> parser json
                    | Choice2Of2 x -> Choice2Of2 x)

            let errors = Commands.getErrors results

            if Seq.isEmpty errors |> not then
                errors |> String.joinLines |> Commands.returnError
            else
                trace "Analysing results..."
                let hits = Commands.getHits results
                let hitCounts = hits |> Sca.hitCountSummary |> List.ofSeq

                trace "Building display..."

                let renderables =
                    seq {
                        hits |> Console.hitsTable

                        if hitCounts |> List.isEmpty |> not then
                            hitCounts |> Console.hitSummaryTable
                    }

                Commands.renderTables renderables

                ReturnCodes.validationOk
