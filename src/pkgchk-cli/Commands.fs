namespace pkgchk

open System
open System.ComponentModel
open System.Diagnostics.CodeAnalysis
open Spectre.Console.Cli

[<ExcludeFromCodeCoverage>]
type PackageCheckCommandSettings() =
    inherit CommandSettings()

    [<CommandArgument(0, "[SOLUTION|PROJECT]")>]
    [<Description("The solution or project file to check.")>]
    [<DefaultValue("")>]
    member val ProjectPath = "" with get, set

    [<CommandOption("-t|--transitive")>]
    [<Description("Toggle transitive package checks. -t true to include them, -t false to exclude.")>]
    [<DefaultValue(true)>]
    member val IncludeTransitives = true with get, set

    [<CommandOption("--deprecated")>]
    [<Description("Check deprecated packagess. -d true to include, -d false to exclude.")>]
    [<DefaultValue(false)>]
    member val IncludeDeprecations = false with get, set

    [<CommandOption("-o|--output")>]
    [<Description("Output directory for reports.")>]
    [<DefaultValue("")>]
    member val OutputDirectory = "" with get, set

    [<CommandOption("-s|--severity")>]
    [<Description("Severity levels to scan for. Matches will return non-zero exit codes. Multiple levels can be specified.")>]
    member val SeverityLevels: string array = [||] with get, set

    [<CommandOption("--trace")>]
    [<Description("Enable trace logging.")>]
    [<DefaultValue(false)>]
    member val TraceLogging = false with get, set

    [<CommandOption("--no-restore")>]
    [<Description("Don't automatically restore packages.")>]
    [<DefaultValue(false)>]
    member val NoRestore = false with get, set

    [<CommandOption("--no-banner", IsHidden = true)>]
    [<Description("Don't show the banner.")>]
    [<DefaultValue(false)>]
    member val NoBanner = false with get, set

[<ExcludeFromCodeCoverage>]
type PackageCheckCommand() =
    inherit Command<PackageCheckCommandSettings>()

    let console = Spectre.Console.AnsiConsole.Console |> Console.send

    let genRestoreArgs (settings: PackageCheckCommandSettings) =
        settings.ProjectPath |> Io.toFullPath |> sprintf "restore %s"

    let genScanArgs (settings: PackageCheckCommandSettings) =
        let projPath = settings.ProjectPath |> Io.toFullPath

        [| yield projPath |> ScaArgs.scanVulnerabilities settings.IncludeTransitives
           if settings.IncludeDeprecations then
               yield projPath |> ScaArgs.scanDeprecations settings.IncludeTransitives |]


    let runProc logging proc =
        try
            proc |> Io.run logging
        finally
            proc.Dispose()

    let runRestore (settings: PackageCheckCommandSettings) logging =
        if settings.NoRestore then
            Choice1Of2 false
        else
            let runRestoreProcParse run proc =
                proc
                |> run
                |> (function
                | Choice2Of2 error -> Choice2Of2 error
                | _ -> Choice1Of2 true)

            settings
            |> genRestoreArgs
            |> Io.createProcess
            |> runRestoreProcParse (runProc logging)


    let runScaProcParse run procs =
        procs
        |> Array.map run
        |> Array.map (function
            | Choice1Of2 json -> Sca.parse json
            | Choice2Of2 x -> Choice2Of2 x)

    let returnError error =
        error |> Console.error |> console
        ReturnCodes.sysError

    let getErrors procResults =
        procResults
        |> Seq.map (function
            | Choice2Of2 x -> x
            | _ -> "")
        |> Seq.filter String.isNotEmpty

    let getHits procResults =
        procResults
        |> Seq.collect (function
            | Choice1Of2 xs -> xs
            | _ -> [])
        |> List.ofSeq

    let getHitCounts (hits: ScaHit list) =

        hits
        |> Seq.groupBy (fun h -> h.kind)
        |> Seq.collect (fun (kind, hs) ->
            hs
            |> Seq.collect (fun h ->
                seq {
                    h.severity
                    yield! h.reasons
                }
                |> Seq.filter String.isNotEmpty)
            |> Seq.groupBy id
            |> Seq.map (fun (s, xs) -> (kind, s, xs |> Seq.length)))

    let reportHitCounts console counts =

        let lines =
            counts |> Seq.map (fun (k, s, c) -> $"{k} - {s}: {c} hits.") |> List.ofSeq

        if lines |> List.isEmpty |> not then
            "Issues found:" |> console
            lines |> String.joinLines |> console

    let returnCode (hits: ScaHit list) =
        match hits with
        | [] -> ReturnCodes.validationOk
        | _ -> ReturnCodes.validationFailed

    let genConsole = Console.generate >> String.joinLines >> console

    let genReport outDir hits =
        let reportFile = outDir |> Io.toFullPath |> Io.combine "pkgchk.md" |> Io.normalise
        hits |> Markdown.generate |> Io.writeFile reportFile
        reportFile

    let trace value = $"[grey]{value}[/]" |> console

    override _.Execute(context, settings) =
        let logging = if settings.TraceLogging then trace else (fun x -> ignore x)

        if settings.NoBanner |> not then
            "[green]Pkgchk-Cli[/]" |> console

        match runRestore settings logging with
        | Choice2Of2 error -> error |> returnError
        | _ ->
            let results =
                settings
                |> genScanArgs
                |> Array.map Io.createProcess
                |> runScaProcParse (runProc logging)

            let errors = getErrors results

            if Seq.isEmpty errors |> not then
                errors |> String.joinLines |> returnError
            else
                let hits = getHits results

                let errorHits = hits |> Sca.hitsByLevels settings.SeverityLevels

                errorHits |> getHitCounts |> reportHitCounts logging

                hits |> genConsole

                if settings.OutputDirectory <> "" then
                    hits |> genReport settings.OutputDirectory |> Console.reportFileBuilt |> console

                errorHits |> returnCode
