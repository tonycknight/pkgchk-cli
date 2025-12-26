namespace pkgchk

open System.ComponentModel
open System.Diagnostics.CodeAnalysis
open Spectre.Console.Cli

[<ExcludeFromCodeCoverage>]
type PackageUpgradeCommandSettings() =
    inherit PackageGithubCommandSettings()

    [<CommandOption("--break-on-upgrades")>]
    [<Description("Break on outstanding package upgrades.")>]
    [<DefaultValue(false)>]
    member val BreakOnUpgrades = false with get, set

[<ExcludeFromCodeCoverage>]
type PackageUpgradeCommand(nuget: Tk.Nuget.INugetClient) =
    inherit AsyncCommand<PackageUpgradeCommandSettings>()

    let genComment (settings: PackageUpgradeCommandSettings, hits, reportImg) =
        let markdown = (hits, reportImg) |> Markdown.generateUpgrades |> String.joinLines

        if markdown.Length < Github.maxCommentSize then
            GithubComment.create settings.GithubSummaryTitle markdown
        else
            GithubComment.create settings.GithubSummaryTitle "_The report is too big for Github - Please check logs_"

    let isSuccessScan (settings: ScanConfiguration, hits: ScaHit list) =
        hits |> List.isEmpty || (not settings.breakOnUpgrades)

    let config (settings: PackageUpgradeCommandSettings) =
        match settings.ConfigFile with
        | x when x <> "" -> x |> Io.fullPath |> Io.normalise |> Config.load
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

    let commandContext trace (settings: PackageUpgradeCommandSettings) =
        { ScaCommandContext.trace = trace
          projectPath = settings.ProjectPath
          includeVulnerabilities = false
          includeTransitives = false
          includeDeprecations = false
          includeDependencies = false
          includeOutdated = true }

    let renderables hits hitCounts =
        seq {
            hits |> Console.hitsTable

            if hitCounts |> List.isEmpty |> not then
                hitCounts |> Console.hitSummaryTable
            else
                pkgchk.Console.green "No upgrades found!" |> CliCommands.console
        }

    override _.Validate
        (context: CommandContext, settings: PackageUpgradeCommandSettings)
        : Spectre.Console.ValidationResult =
        settings.Validate()

    override _.ExecuteAsync(context, settings, cancellationToken) =
        task {
            let trace = CliCommands.trace settings.TraceLogging
            let config = config settings

            if not config.noBanner then
                CliCommands.renderBanner nuget

            match DotNet.restore config settings.ProjectPath trace with
            | Choice2Of2 error -> return error |> CliCommands.returnError
            | _ ->
                let ctx = commandContext trace settings

                let results = DotNet.scan ctx

                trace "Analysing results..."
                let hits = ScaModels.getHits results |> Config.filterPackages config
                let hitCounts = hits |> ScaModels.hitCountSummary |> List.ofSeq
                let isSuccess = isSuccessScan (config, hits)
                let errors = DotNet.scanErrors results

                if Seq.isEmpty errors |> not then
                    return errors |> String.joinLines |> CliCommands.returnError
                else
                    trace "Building display..."

                    renderables hits hitCounts |> CliCommands.renderTables

                    let reportImg =
                        match isSuccess with
                        | true -> settings.GoodImageUri
                        | false -> settings.BadImageUri

                    if settings.OutputDirectory <> "" then
                        trace "Building reports..."

                        (hits, reportImg)
                        |> Markdown.generateUpgrades
                        |> Io.writeFile ("pkgchk-upgrades.md" |> Io.composeFilePath settings.OutputDirectory)
                        |> CliCommands.renderReportLine

                    if settings.HasGithubParamters() then
                        trace "Building Github reports..."
                        let comment = genComment (settings, hits, reportImg)

                        if String.isNotEmpty settings.GithubPrId then
                            do! Github.sendPrComment settings trace comment

                        if String.isNotEmpty settings.GithubCommit then
                            do! Github.sendCheck settings trace isSuccess comment

                    return isSuccess |> CliCommands.returnCode
        }
