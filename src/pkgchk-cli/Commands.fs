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
    [<DefaultValue([| "High"; "Critical"; "Critical Bugs" |])>]
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

    [<CommandOption("--github-token", IsHidden = true)>]
    [<Description("A Github token.")>]
    [<DefaultValue("")>]
    member val GithubToken = "" with get, set

    [<CommandOption("--github-repo", IsHidden = true)>]
    [<Description("The name of the Github repository in the form <owner>/<repo>, e.g. github/octokit.")>]
    [<DefaultValue("")>]
    member val GithubRepo = "" with get, set

    [<CommandOption("--github-title", IsHidden = true)>]
    [<Description("The Github report title.")>]
    [<DefaultValue("")>]
    member val GithubSummaryTitle = "" with get, set

    [<CommandOption("--github-pr", IsHidden = true)>]
    [<Description("Pull request ID.")>]
    [<DefaultValue("")>]
    member val GithubPrId = "" with get, set

[<ExcludeFromCodeCoverage>]
type PackageCheckCommand(nuget: Tk.Nuget.INugetClient) =
    inherit Command<PackageCheckCommandSettings>()

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

    let rec genComment trace (settings: PackageCheckCommandSettings, hits, errorHits, hitCounts) attempt =
        let markdown =
            (hits, errorHits, hitCounts, settings.SeverityLevels)
            |> Markdown.generate
            |> String.joinLines

        if markdown.Length < Github.maxCommentSize then
            GithubComment.create settings.GithubSummaryTitle markdown
        else
            trace $"Shrinking Github output as too large (attempt #{attempt + 1})..."

            if attempt >= 1 then
                GithubComment.create settings.GithubSummaryTitle "_The report's too big for Github - Please check logs_"
            else
                genComment trace (settings, [], errorHits, hitCounts) (attempt + 1)

    let returnCode (hits: ScaHit list) =
        match hits with
        | [] -> ReturnCodes.validationOk
        | _ -> ReturnCodes.validationFailed

    let reportFile outDir =
        outDir |> Io.toFullPath |> Io.combine "pkgchk.md" |> Io.normalise

    let validateSettings (settings: PackageCheckCommandSettings) =
        if String.isNotEmpty settings.GithubPrId then
            if String.isEmpty settings.GithubToken then
                failwith "Missing Github token."

            if String.isEmpty settings.GithubRepo then
                failwith "Missing Github repository. Use the form <owner>/<name>."

            let repo = Github.repo settings.GithubRepo

            if repo |> fst |> String.isEmpty then
                failwith "The repository owner is missing. Use the form <owner>/<name>."

            if repo |> snd |> String.isEmpty then
                failwith "The repository name is missing. Use the form <owner>/<name>."

            if String.isInt settings.GithubPrId |> not then
                failwith "The PR ID must be an integer."

    override _.Execute(context, settings) =
        let trace = trace settings.TraceLogging

        settings.SeverityLevels <- settings.SeverityLevels |> Array.filter String.isNotEmpty

        if settings.NoBanner |> not then
            nuget |> App.banner |> console

        validateSettings settings

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
                    && String.isNotEmpty settings.GithubRepo
                    && String.isNotEmpty settings.GithubPrId
                then
                    trace "Building Github reports..."
                    let prId = String.toInt settings.GithubPrId
                    let repo = Github.repo settings.GithubRepo
                    let client = Github.client settings.GithubToken

                    let comment = genComment trace (settings, hits, errorHits, hitCounts) 0

                    trace $"Posting {comment.title} report to Github repo {repo}..."
                    let _ = (comment |> Github.setPrComment client repo prId).Result
                    $"{comment.title} report sent to Github." |> Console.italic |> console

                errorHits |> returnCode
