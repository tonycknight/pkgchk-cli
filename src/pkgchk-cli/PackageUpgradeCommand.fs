namespace pkgchk

open System
open System.ComponentModel
open System.Diagnostics.CodeAnalysis
open Spectre.Console.Cli

[<ExcludeFromCodeCoverage>]
type PackageUpgradeCommandSettings() =
    inherit PackageGithubCommandSettings()

    [<CommandOption("-o|--output")>]
    [<Description("Output directory for reports.")>]
    [<DefaultValue("")>]
    member val OutputDirectory = "" with get, set

    [<CommandOption("--break-on-upgrades")>]
    [<Description("Break on outstanding package upgrades.")>]
    [<DefaultValue(false)>]
    member val BreakOnUpgrades = false with get, set

[<ExcludeFromCodeCoverage>]
type PackageUpgradeCommand(nuget: Tk.Nuget.INugetClient) =
    inherit Command<PackageUpgradeCommandSettings>()

    let genComment (settings: PackageUpgradeCommandSettings, hits, reportImg) =
        let markdown = (hits, reportImg) |> Markdown.generateUpgrades |> String.joinLines

        if markdown.Length < Github.maxCommentSize then
            GithubComment.create settings.GithubSummaryTitle markdown
        else
            GithubComment.create settings.GithubSummaryTitle "_The report is too big for Github - Please check logs_"

    let isSuccessScan (settings: ScanConfiguration, hits: ScaHit list) =
        match hits with
        | [] -> true
        | _ -> not settings.breakOnUpgrades

    let returnCode (settings: ScanConfiguration, hits: ScaHit list) =
        match isSuccessScan (settings, hits) with
        | true -> ReturnCodes.validationOk
        | false -> ReturnCodes.validationFailed

    let config (settings: PackageUpgradeCommandSettings) =
        match settings.ConfigFile with
        | x when x <> "" -> x |> Io.toFullPath |> Io.normalise |> Config.load
        | _ ->
            { pkgchk.ScanConfiguration.includedPackages = settings.IncludedPackages
              excludedPackages = settings.ExcludedPackages
              breakOnUpgrades = settings.BreakOnUpgrades
              noBanner = settings.NoBanner
              noRestore = settings.NoRestore
              severities = [||]
              breakOnVulnerabilities = false
              breakOnDeprecations = false
              checkTransitives = false }

    override _.Execute(context, settings) =
        let trace = CliCommands.trace settings.TraceLogging
        let config = config settings

        if config.noBanner |> not then
            nuget |> App.banner |> CliCommands.console

        settings.Validate()

        match Sca.restore config settings.ProjectPath trace with
        | Choice2Of2 error -> error |> CliCommands.returnError
        | _ ->
            let ctx =
                { ScaCommandContext.trace = trace
                  projectPath = settings.ProjectPath
                  includeVulnerabilities = false
                  includeTransitives = false
                  includeDeprecations = false
                  includeDependencies = false
                  includeOutdated = true }

            let results = Sca.scan ctx

            let errors = CliCommands.getErrors results

            if Seq.isEmpty errors |> not then
                errors |> String.joinLines |> CliCommands.returnError
            else
                trace "Analysing results..."

                let hits = ScaModels.getHits results |> Config.filterPackages config

                let hitCounts = hits |> ScaModels.hitCountSummary |> List.ofSeq

                trace "Building display..."

                let renderables =
                    seq {
                        hits |> Console.hitsTable

                        if hitCounts |> List.isEmpty |> not then
                            hitCounts |> Console.hitSummaryTable
                        else
                            pkgchk.Console.green "No upgrades found!" |> CliCommands.console
                    }

                CliCommands.renderTables renderables

                let reportImg =
                    match isSuccessScan (config, hits) with
                    | true -> settings.GoodImageUri
                    | false -> settings.BadImageUri

                if settings.OutputDirectory <> "" then
                    trace "Building reports..."
                    let reportFile = Io.toFullPath >> Io.combine "pkgchk-upgrades.md" >> Io.normalise

                    let reportFile =
                        (hits, reportImg)
                        |> Markdown.generateUpgrades
                        |> Io.writeFile (reportFile settings.OutputDirectory)

                    $"{Environment.NewLine}Report file [link={reportFile}]{reportFile}[/] built."
                    |> Console.italic
                    |> CliCommands.console

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

                    let comment = genComment (settings, hits, reportImg)

                    if String.isNotEmpty settings.GithubPrId then
                        trace $"Posting {comment.title} PR comment to Github repo {repo}..."
                        let _ = (comment |> Github.setPrComment trace client repo prId).Result
                        $"{comment.title} report sent to Github." |> Console.italic |> CliCommands.console

                    if String.isNotEmpty settings.GithubCommit then
                        trace $"Posting {comment.title} build check to Github repo {repo}..."
                        let isSuccess = isSuccessScan (config, hits)

                        (comment |> Github.createCheck trace client repo commit isSuccess).Result
                        |> ignore

                returnCode (config, hits)
