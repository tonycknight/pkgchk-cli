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
    [<DefaultValue([| "High"; "Critical"; "Critical Bugs"; "Legacy" |])>]
    member val SeverityLevels: string array = [||] with get, set

    [<CommandOption("--trace")>]
    [<Description("Enable trace logging.")>]
    [<DefaultValue(false)>]
    member val TraceLogging = false with get, set

    [<CommandOption("--no-restore")>]
    [<Description("Don't automatically restore packages.")>]
    [<DefaultValue(false)>]
    member val NoRestore = false with get, set

    [<CommandOption("--no-banner")>]
    [<Description("Don't show the banner.")>]
    [<DefaultValue(false)>]
    member val NoBanner = false with get, set

[<ExcludeFromCodeCoverage>]
type PackageCheckCommand() =
    inherit Command<PackageCheckCommandSettings>()

    let console = Spectre.Console.AnsiConsole.MarkupLine

    let trace traceLogging =
        if traceLogging then
            (Console.markup "grey" >> console)
        else
            ignore

    let renderTables (values: seq<Spectre.Console.Table>) =
        values |> Seq.iter Spectre.Console.AnsiConsole.Write

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

            settings.ProjectPath
            |> Sca.restoreArgs
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
        |> Seq.distinct

    let getHits procResults =
        procResults
        |> Seq.collect (function
            | Choice1Of2 xs -> xs
            | _ -> [])
        |> List.ofSeq

    let returnCode (hits: ScaHit list) =
        match hits with
        | [] -> ReturnCodes.validationOk
        | _ -> ReturnCodes.validationFailed

    let reportFile outDir =
        outDir |> Io.toFullPath |> Io.combine "pkgchk.md" |> Io.normalise

    override _.Execute(context, settings) =
        let trace = trace settings.TraceLogging

        if settings.NoBanner |> not then
            seq {
                Console.cyan "Pkgchk-Cli"
                App.version () |> Option.defaultValue "unknown" |> Console.yellow |> sprintf "Version %s"

                App.repo
                |> Console.cyan
                |> sprintf "For more information, see %s"
                |> Console.italic
            }
            |> Seq.iter console

        match runRestore settings trace with
        | Choice2Of2 error -> error |> returnError
        | _ ->
            let results =
                (settings.ProjectPath, settings.IncludeTransitives, settings.IncludeDeprecations)
                |> Sca.scanArgs
                |> Array.map Io.createProcess
                |> runScaProcParse (runProc trace)

            let errors = getErrors results

            if Seq.isEmpty errors |> not then
                errors |> String.joinLines |> returnError
            else
                trace "Analysing results..."
                let hits = getHits results
                let errorHits = hits |> Sca.hitsByLevels settings.SeverityLevels
                let hitCounts = errorHits |> Sca.hitCountSummary |> List.ofSeq

                trace "Building display..."

                let renderables =
                    seq {
                        hits |> Console.hitsTable
                        errorHits |> Console.headlineTable

                        if hitCounts |> List.isEmpty |> not then
                            settings.SeverityLevels |> Console.severitySettingsTable
                            hitCounts |> Console.hitSummaryTable
                    }

                renderables |> renderTables

                if settings.OutputDirectory <> "" then
                    trace "Building reports..."

                    let reportFile =
                        (hits, errorHits, hitCounts, settings.SeverityLevels)
                        |> Markdown.generate
                        |> Io.writeFile (reportFile settings.OutputDirectory)

                    $"{Environment.NewLine}Report file [link={reportFile}]{reportFile}[/] built."
                    |> Console.italic
                    |> console

                errorHits |> returnCode
