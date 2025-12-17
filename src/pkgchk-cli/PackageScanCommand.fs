namespace pkgchk

open System
open System.ComponentModel
open System.Diagnostics.CodeAnalysis
open Spectre.Console.Cli

[<ExcludeFromCodeCoverage>]
type PackageScanCommandSettings() =
    inherit PackageCommandSettings()

    [<CommandOption("-v|--vulnerable")>]
    [<Description("Toggle vulnerable package checks. true to include them, false to exclude.")>]
    [<DefaultValue(true)>]
    member val IncludeVulnerables = true with get, set

    [<CommandOption("-t|--transitive")>]
    [<Description("Toggle transitive package checks. true to include them, false to exclude.")>]
    [<DefaultValue(true)>]
    member val IncludeTransitives = true with get, set

    [<CommandOption("-d|--deprecated")>]
    [<Description("Check deprecated packagess. true to include, false to exclude.")>]
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

    [<CommandOption("--github-commit", IsHidden = true)>]
    [<Description("Commit hash.")>]
    [<DefaultValue("")>]
    member val GithubCommit = "" with get, set

    [<CommandOption("--pass-img", IsHidden = true)>]
    [<Description("URI of an image for successful scans.")>]
    [<DefaultValue("")>]
    member val GoodImageUri = "" with get, set

    [<CommandOption("--fail-img", IsHidden = true)>]
    [<Description("URI of an image for failed scans.")>]
    [<DefaultValue("")>]
    member val BadImageUri = "" with get, set

    [<CommandOption("--config", IsHidden = false)>]
    [<Description("Configuration file path.")>]
    [<DefaultValue("")>]
    member val ConfigFile = "" with get, set

[<ExcludeFromCodeCoverage>]
type PackageScanCommand(nuget: Tk.Nuget.INugetClient) =
    inherit Command<PackageScanCommandSettings>()

    let rec genComment trace (settings: PackageScanCommandSettings, hits, errorHits, hitCounts, imageUri) attempt =
        let markdown =
            (hits, errorHits, hitCounts, settings.SeverityLevels, imageUri)
            |> Markdown.generate
            |> String.joinLines

        let summaryTitle = settings.GithubSummaryTitle

        if markdown.Length < Github.maxCommentSize then
            GithubComment.create summaryTitle markdown
        else
            trace $"Shrinking Github output as too large (attempt #{attempt + 1})..."

            if attempt >= 1 then
                GithubComment.create summaryTitle "_The report is too big for Github - Please check logs_"
            else
                genComment trace (settings, [], errorHits, hitCounts, imageUri) (attempt + 1)

    let isSuccessScan (hits: ScaHit list) =
        match hits with
        | [] -> true
        | _ -> false

    let returnCode (hits: ScaHit list) =
        match isSuccessScan hits with
        | true -> ReturnCodes.validationOk
        | _ -> ReturnCodes.validationFailed

    let reportFile outDir =
        outDir |> Io.toFullPath |> Io.combine "pkgchk.md" |> Io.normalise

    let cleanSettings (settings: PackageScanCommandSettings) =
        settings.SeverityLevels <- settings.SeverityLevels |> Array.filter String.isNotEmpty
        settings

    let validateSettings (settings: PackageScanCommandSettings) =
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

    let config (settings: PackageScanCommandSettings) =
        match settings.ConfigFile with
        | x when x <> "" -> x |> Io.toFullPath |> Io.normalise |> Config.load
        | x ->
            { pkgchk.ScanConfiguration.includedPackages = [||]
              excludedPackages = [||]
              breakOnUpgrades = false
              noBanner = settings.NoBanner
              severities = settings.SeverityLevels
              breakOnVulnerabilities = settings.IncludeVulnerables
              breakOnDeprecations = settings.IncludeDeprecations
              checkTransitives = settings.IncludeTransitives }

    override _.Execute(context, settings) =
        let trace = Commands.trace settings.TraceLogging

        let settings = cleanSettings settings

        let config = config settings

        if config.noBanner |> not then
            nuget |> App.banner |> Commands.console

        validateSettings settings

        match Commands.restore settings trace with
        | Choice2Of2 error -> error |> Commands.returnError
        | _ ->
            let results =
                (settings.ProjectPath,
                 config.breakOnVulnerabilities,
                 config.checkTransitives,
                 config.breakOnDeprecations,
                 false,
                 false)
                |> Commands.scan trace

            let errors = Commands.getErrors results

            if Seq.isEmpty errors |> not then
                errors |> String.joinLines |> Commands.returnError
            else
                trace "Analysing results..."
                let hits = Commands.getHits results

                let errorHits = hits |> Sca.hitsByLevels config.severities
                let hitCounts = errorHits |> Sca.hitCountSummary |> List.ofSeq

                trace "Building display..."

                let renderables =
                    seq {
                        hits |> Console.hitsTable

                        if config.breakOnVulnerabilities || config.breakOnDeprecations then
                            errorHits |> Console.headlineTable

                        if hitCounts |> List.isEmpty |> not then
                            config.severities |> Console.severitySettingsTable
                            hitCounts |> Console.hitSummaryTable
                    }

                renderables |> Commands.renderTables

                let reportImg =
                    match isSuccessScan errorHits with
                    | true -> settings.GoodImageUri
                    | false -> settings.BadImageUri

                if settings.OutputDirectory <> "" then
                    trace "Building reports..."

                    let reportFile =
                        (hits, errorHits, hitCounts, config.severities, reportImg)
                        |> Markdown.generate
                        |> Io.writeFile (reportFile settings.OutputDirectory)

                    $"{Environment.NewLine}Report file [link={reportFile}]{reportFile}[/] built."
                    |> Console.italic
                    |> Commands.console

                if
                    String.isNotEmpty settings.GithubToken
                    && String.isNotEmpty settings.GithubRepo
                    && (String.isNotEmpty settings.GithubPrId || String.isNotEmpty settings.GithubCommit)
                then
                    trace "Building Github reports..."
                    let prId = String.toInt settings.GithubPrId
                    let repo = Github.repo settings.GithubRepo
                    let client = Github.client settings.GithubToken
                    let commit = settings.GithubCommit

                    let comment = genComment trace (settings, hits, errorHits, hitCounts, reportImg) 0

                    if String.isNotEmpty settings.GithubPrId then
                        trace $"Posting {comment.title} PR comment to Github repo {repo}..."
                        let _ = (comment |> Github.setPrComment trace client repo prId).Result
                        $"{comment.title} report sent to Github." |> Console.italic |> Commands.console

                    if String.isNotEmpty settings.GithubCommit then
                        trace $"Posting {comment.title} build check to Github repo {repo}..."
                        let isSuccess = isSuccessScan errorHits

                        (comment |> Github.createCheck trace client repo commit isSuccess).Result
                        |> ignore


                errorHits |> returnCode
