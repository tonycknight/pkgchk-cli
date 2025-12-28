namespace pkgchk

open System.Diagnostics.CodeAnalysis
open Spectre.Console.Cli

[<ExcludeFromCodeCoverage>]
type PackageUpgradeCommand(nuget: Tk.Nuget.INugetClient) =
    inherit AsyncCommand<PackageUpgradeCommandSettings>()

    let genComment (context: ApplicationContext, hits, reportImg) =
        let markdown = (hits, reportImg) |> Markdown.generateUpgrades |> String.joinLines

        if markdown.Length < Github.maxCommentSize then
            GithubComment.create context.github.summaryTitle markdown
        else
            GithubComment.create context.github.summaryTitle "_The report is too big for Github - Please check logs_"

    let isSuccessScan (context: ApplicationContext, hits: ScaHit list) =
        hits |> List.isEmpty || (context.options.breakOnUpgrades |> not)

    let appContext (settings: PackageUpgradeCommandSettings) =
        let context = Context.upgradesContext settings

        { context with
            options = Context.loadApplyConfig context.options }

    let dotnetContext (context: ApplicationContext) =
        { DotNetScanContext.trace = context.services.trace
          projectPath = context.options.projectPath
          includeVulnerabilities = false
          includeTransitives = false
          includeDeprecations = false
          includeDependencies = false
          includeOutdated = true }

    let consoleTable hits hitCounts =
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
                let scanResults = context |> dotnetContext |> DotNet.scan

                context.services.trace "Analysing results..."
                let errors = DotNet.getErrors scanResults

                if Seq.isEmpty errors |> not then
                    return errors |> String.joinLines |> CliCommands.returnError
                else

                    let hits =
                        DotNet.getHits scanResults
                        |> Context.filterPackages context.options
                        |> List.ofSeq

                    let hitCounts = hits |> ScaModels.hitCountSummary |> List.ofSeq
                    let isSuccess = isSuccessScan (context, hits)

                    context.services.trace "Building display..."
                    consoleTable hits hitCounts |> CliCommands.renderTables

                    let reportImg = context |> Context.reportImage isSuccess

                    if context.report.reportDirectory <> "" then
                        context.services.trace "Building reports..."

                        (hits, reportImg)
                        |> Markdown.generateUpgrades
                        |> Io.writeFile ("pkgchk-upgrades.md" |> Io.composeFilePath context.report.reportDirectory)
                        |> CliCommands.renderReportLine

                    if Context.hasGithubParameters context then
                        context.services.trace "Building Github reports..."
                        let comment = genComment (context, hits, reportImg)

                        if String.isNotEmpty context.github.prId then
                            do! Github.sendPrComment context comment

                        if String.isNotEmpty context.github.commit then
                            do! Github.sendCheck context isSuccess comment

                    return isSuccess |> CliCommands.returnCode
        }
