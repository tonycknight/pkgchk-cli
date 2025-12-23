namespace pkgchk

open System
open System.ComponentModel
open System.Diagnostics.CodeAnalysis
open Spectre.Console.Cli

[<ExcludeFromCodeCoverage>]
type PackageScanCommandSettings() =
    inherit PackageGithubCommandSettings()

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

[<ExcludeFromCodeCoverage>]
type PackageScanCommand(nuget: Tk.Nuget.INugetClient) =
    inherit Command<PackageScanCommandSettings>()

    let rec genComment trace (settings: PackageScanCommandSettings, hits, errorHits, hitCounts, imageUri) attempt =
        let markdown =
            (hits, errorHits, hitCounts, settings.SeverityLevels, imageUri)
            |> Markdown.generateScan
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

    let isSuccessScan (hits: ScaHit list) = hits |> List.isEmpty

    let cleanSettings (settings: PackageScanCommandSettings) =
        settings.SeverityLevels <- settings.SeverityLevels |> Array.filter String.isNotEmpty
        settings

    let config (settings: PackageScanCommandSettings) =
        match settings.ConfigFile with
        | x when x <> "" -> x |> Io.fullPath |> Io.normalise |> Config.load
        | _ ->
            { pkgchk.ScanConfiguration.includedPackages = settings.IncludedPackages
              excludedPackages = settings.ExcludedPackages
              breakOnUpgrades = false
              noBanner = settings.NoBanner
              noRestore = settings.NoRestore
              severities = settings.SeverityLevels
              breakOnVulnerabilities = settings.IncludeVulnerables
              breakOnDeprecations = settings.IncludeDeprecations
              checkTransitives = settings.IncludeTransitives }

    let commandContext trace (settings: PackageScanCommandSettings) config =
        { ScaCommandContext.trace = trace
          projectPath = settings.ProjectPath
          includeVulnerabilities = config.breakOnVulnerabilities
          includeTransitives = config.checkTransitives
          includeDeprecations = config.breakOnDeprecations
          includeDependencies = false
          includeOutdated = false }

    let renderables config hits hitCounts errorHits =
        seq {
            hits |> Console.hitsTable
            let mutable headlineSet = false

            if config.breakOnVulnerabilities || config.breakOnDeprecations then
                errorHits |> Console.vulnerabilityHeadlineTable
                headlineSet <- true

            if hitCounts |> List.isEmpty |> not then
                config.severities |> Console.severitySettingsTable
                hitCounts |> Console.hitSummaryTable

            else if (not headlineSet) then
                Console.noscanHeadlineTable ()
        }

    override _.Execute(context, settings) =
        let trace = CliCommands.trace settings.TraceLogging

        let settings = cleanSettings settings

        let config = config settings

        if not config.noBanner then
            CliCommands.renderBanner nuget

        settings.Validate()

        match DotNet.restore config settings.ProjectPath trace with
        | Choice2Of2 error -> error |> CliCommands.returnError
        | _ ->
            let ctx = commandContext trace settings config

            let results = DotNet.scan ctx

            let errors = CliCommands.getErrors results

            if Seq.isEmpty errors |> not then
                errors |> String.joinLines |> CliCommands.returnError
            else
                trace "Analysing results..."
                let hits = ScaModels.getHits results |> Config.filterPackages config

                let errorHits = hits |> ScaModels.hitsByLevels config.severities
                let hitCounts = errorHits |> ScaModels.hitCountSummary |> List.ofSeq

                trace "Building display..."

                renderables config hits hitCounts errorHits |> CliCommands.renderTables

                let reportImg =
                    match isSuccessScan errorHits with
                    | true -> settings.GoodImageUri
                    | false -> settings.BadImageUri

                if settings.OutputDirectory <> "" then
                    trace "Building reports..."

                    (hits, errorHits, hitCounts, config.severities, reportImg)
                    |> Markdown.generateScan
                    |> Io.writeFile ("pkgchk.md" |> Io.composeFilePath settings.OutputDirectory)
                    |> CliCommands.renderReportLine

                if
                    String.isNotEmpty settings.GithubToken
                    && String.isNotEmpty settings.GithubRepo
                    && (String.isNotEmpty settings.GithubPrId || String.isNotEmpty settings.GithubCommit)
                then
                    trace "Building Github reports..."
                    let prId = String.toInt settings.GithubPrId
                    let repo = String.split '/' settings.GithubRepo
                    let client = Github.client settings.GithubToken
                    let commit = settings.GithubCommit

                    let comment = genComment trace (settings, hits, errorHits, hitCounts, reportImg) 0

                    if String.isNotEmpty settings.GithubPrId then
                        trace $"Posting {comment.title} PR comment to Github repo {repo}..."
                        let _ = (comment |> Github.setPrComment trace client repo prId).Result

                        $"{comment.title} report sent to Github."
                        |> Console.italic
                        |> CliCommands.console

                    if String.isNotEmpty settings.GithubCommit then
                        trace $"Posting {comment.title} build check to Github repo {repo}..."
                        let isSuccess = isSuccessScan errorHits

                        (comment |> Github.createCheck trace client repo commit isSuccess).Result
                        |> ignore


                errorHits |> isSuccessScan |> CliCommands.returnCode
