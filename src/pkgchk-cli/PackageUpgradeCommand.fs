namespace pkgchk

open System.Diagnostics.CodeAnalysis
open Spectre.Console.Cli

[<ExcludeFromCodeCoverage>]
type PackageUpgradeCommand(nuget: Tk.Nuget.INugetClient) =
    inherit AsyncCommand<PackageUpgradeCommandSettings>()

    let genComment (settings: PackageUpgradeCommandSettings, hits, reportImg) =
        let markdown = (hits, reportImg) |> Markdown.generateUpgrades |> String.joinLines

        if markdown.Length < Github.maxCommentSize then
            GithubComment.create settings.GithubSummaryTitle markdown
        else
            GithubComment.create settings.GithubSummaryTitle "_The report is too big for Github - Please check logs_"

    let isSuccessScan (context: ApplicationContext, hits: ScaHit list) =
        hits |> List.isEmpty || (context.options.breakOnUpgrades |> not)

    let appContext (settings: PackageUpgradeCommandSettings) = 
        let context = Context.upgradesContext settings

        { context with options = Context.loadApplyConfig context.options }

    let commandContext trace (context: ApplicationContext) =
        { ScaCommandContext.trace = trace
          projectPath = context.options.projectPath
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
            let context = appContext settings

            if context.options.suppressBanner |> not then
                CliCommands.renderBanner nuget

            match DotNet.restore context with
            | Choice2Of2 error -> return error |> CliCommands.returnError
            | _ ->
                let results = context |> commandContext context.services.trace |> DotNet.scan

                context.services.trace "Analysing results..."
                let hits = ScaModels.getHits results |> Context.filterPackages context.options |> List.ofSeq
                let hitCounts = hits |> ScaModels.hitCountSummary |> List.ofSeq
                let isSuccess = isSuccessScan (context, hits)
                let errors = DotNet.scanErrors results

                if Seq.isEmpty errors |> not then
                    return errors |> String.joinLines |> CliCommands.returnError
                else
                    context.services.trace "Building display..."

                    renderables hits hitCounts |> CliCommands.renderTables

                    let reportImg =
                        match isSuccess with
                        | true -> context.report.goodImageUri
                        | false -> context.report.badImageUri

                    if context.report.reportDirectory <> "" then
                        context.services.trace "Building reports..."

                        (hits, reportImg)
                        |> Markdown.generateUpgrades
                        |> Io.writeFile ("pkgchk-upgrades.md" |> Io.composeFilePath context.report.reportDirectory)
                        |> CliCommands.renderReportLine

                    if settings.HasGithubParamters() then
                        context.services.trace "Building Github reports..."
                        let comment = genComment (settings, hits, reportImg)

                        if String.isNotEmpty context.github.prId then
                            do! Github.sendPrComment settings context.services.trace comment

                        if String.isNotEmpty context.github.commit then
                            do! Github.sendCheck settings context.services.trace isSuccess comment

                    return isSuccess |> CliCommands.returnCode
        }
