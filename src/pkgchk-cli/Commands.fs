﻿namespace pkgchk

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

    [<CommandOption("--vulnerable")>]
    [<Description("Toggle vulnerable package checks. -t true to include them, -t false to exclude.")>]
    [<DefaultValue(true)>]
    member val IncludeVulnerables = true with get, set

    [<CommandOption("-t|--transitive")>]
    [<Description("Toggle transitive package checks. -t true to include them, -t false to exclude.")>]
    [<DefaultValue(true)>]
    member val IncludeTransitives = true with get, set

    [<CommandOption("--deprecated")>]
    [<Description("Check deprecated packagess. -d true to include, -d false to exclude.")>]
    [<DefaultValue(false)>]
    member val IncludeDeprecations = false with get, set

    [<CommandOption("--dependencies")>]
    [<Description("List all dependency packagess. -d true to include, -d false to exclude.")>]
    [<DefaultValue(false)>]
    member val IncludeDependencies = false with get, set

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

    [<CommandOption("--githubtoken")>]
    [<Description("A Github token.")>]
    [<DefaultValue("")>]
    member val GithubToken = "" with get, set

    [<CommandOption("--repoowner")>]
    [<Description("The name of the Github repository owner.")>]
    [<DefaultValue("")>]
    member val GithubRepoOwner = "" with get, set

    [<CommandOption("--repo")>]
    [<Description("The name of the Github repository.")>]
    [<DefaultValue("")>]
    member val GithubRepo = "" with get, set

    [<CommandOption("--title")>]
    [<Description("The Github PR report title.")>]
    [<DefaultValue("")>]
    member val SummaryTitle = "" with get, set

    [<CommandOption("--pr")>]
    [<Description("Pull request ID.")>]
    [<DefaultValue(0)>]
    member val PrId = 0 with get, set

[<ExcludeFromCodeCoverage>]
type PackageCheckCommand(nuget: Tk.Nuget.INugetClient) =
    inherit Command<PackageCheckCommandSettings>()

    let console = Spectre.Console.AnsiConsole.MarkupLine

    let trace traceLogging =
        if traceLogging then
            (Console.markup Rendering.grey >> console)
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

    let returnCode (hits: ScaHit list) =
        match hits with
        | [] -> ReturnCodes.validationOk
        | _ -> ReturnCodes.validationFailed

    let reportFile outDir =
        outDir |> Io.toFullPath |> Io.combine "pkgchk.md" |> Io.normalise

    override _.Execute(context, settings) =
        let trace = trace settings.TraceLogging

        if settings.NoBanner |> not then
            nuget |> App.banner |> console

        if settings.PrId <> 0 then
            if String.isEmpty settings.GithubToken then
                failwith "Missing Github token"

            if String.isEmpty settings.GithubRepoOwner then
                failwith "Missing Github owner"

            if String.isEmpty settings.GithubRepo then
                failwith "Missing Github repo"

        match runRestore settings trace with
        | Choice2Of2 error -> error |> returnError
        | _ ->
            let results =
                (settings.ProjectPath,
                 settings.IncludeVulnerables,
                 settings.IncludeTransitives,
                 settings.IncludeDeprecations,
                 settings.IncludeDependencies)
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
                let errorHits = hits |> Sca.hitsByLevels settings.SeverityLevels
                let hitCounts = errorHits |> Sca.hitCountSummary |> List.ofSeq

                trace "Building display..."

                let renderables =
                    seq {
                        hits |> Console.hitsTable

                        if settings.IncludeVulnerables || settings.IncludeDeprecations then
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

                if
                    String.isNotEmpty settings.GithubToken
                    && String.isNotEmpty settings.GithubRepoOwner
                    && String.isNotEmpty settings.GithubRepo
                then
                    // if PR ID is present, build ...
                    let client = Github.client settings.GithubToken
                    let repo = (settings.GithubRepoOwner, settings.GithubRepo)

                    let title =
                        if String.isEmpty settings.SummaryTitle then
                            "pkgchk summary"
                        else
                            settings.SummaryTitle

                    if settings.PrId <> 0 then
                        trace "Building Github summary..."

                        let markdown =
                            (hits, errorHits, hitCounts, settings.SeverityLevels)
                            |> Markdown.generate
                            |> String.joinLines

                        trace $"Posting {title} report to Github..."

                        let comment =
                            { GithubComment.title = $"# {title}"
                              body = markdown }

                        let x = (comment |> Github.setPrComment client repo settings.PrId).Result
                        trace $"Sent {title} report to Github."


                    // TODO: build???
                    ignore 0

                errorHits |> returnCode
