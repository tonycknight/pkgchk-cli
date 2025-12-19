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

    [<CommandOption("-i|--included-package", IsHidden = false)>]
    [<Description("The name of a package to include in the scan. Multiple packages can be specified.")>]
    member val IncludedPackages: string[] = [||] with get, set

    [<CommandOption("-x|--excluded-package", IsHidden = false)>]
    [<Description("The name of a package to exclude from the scan. Multiple packages can be specified.")>]
    member val ExcludedPackages: string[] = [||] with get, set

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
        match (settings.IncludedPackages, settings.ExcludedPackages, settings.ConfigFile) with
        | ([||], [||], x) when x <> "" -> x |> Io.toFullPath |> Io.normalise |> Config.load
        | _ ->
            { pkgchk.ScanConfiguration.includedPackages = settings.IncludedPackages
              excludedPackages = settings.ExcludedPackages
              breakOnUpgrades = settings.BreakOnUpgrades
              noBanner = settings.NoBanner
              severities = [||]
              breakOnVulnerabilities = false
              breakOnDeprecations = false
              checkTransitives = false }

    override _.Execute(context, settings) =
        let trace = Commands.trace settings.TraceLogging
        let config = config settings

        if config.noBanner |> not then
            nuget |> App.banner |> Commands.console

        settings.Validate()

        match Commands.restore settings trace with
        | Choice2Of2 error -> error |> Commands.returnError
        | _ ->
            let results =
                (settings.ProjectPath, false, false, false, false, true) |> Commands.scan trace

            let errors = Commands.getErrors results

            if Seq.isEmpty errors |> not then
                errors |> String.joinLines |> Commands.returnError
            else
                trace "Analysing results..."

                let hits = Commands.getHits results |> Config.filterPackages config

                let hitCounts = hits |> Sca.hitCountSummary |> List.ofSeq

                trace "Building display..."

                let renderables =
                    seq {
                        hits |> Console.hitsTable

                        if hitCounts |> List.isEmpty |> not then
                            hitCounts |> Console.hitSummaryTable
                        else
                            pkgchk.Console.green "No upgrades found!" |> Commands.console
                    }

                Commands.renderTables renderables

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

                    let comment = genComment (settings, hits, reportImg)

                    if String.isNotEmpty settings.GithubPrId then
                        trace $"Posting {comment.title} PR comment to Github repo {repo}..."
                        let _ = (comment |> Github.setPrComment trace client repo prId).Result
                        $"{comment.title} report sent to Github." |> Console.italic |> Commands.console

                    if String.isNotEmpty settings.GithubCommit then
                        trace $"Posting {comment.title} build check to Github repo {repo}..."
                        let isSuccess = isSuccessScan (config, hits)

                        (comment |> Github.createCheck trace client repo commit isSuccess).Result
                        |> ignore

                returnCode (config, hits)
