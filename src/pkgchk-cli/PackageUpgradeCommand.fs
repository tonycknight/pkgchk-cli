namespace pkgchk

open System.Diagnostics.CodeAnalysis
open Spectre.Console.Cli

[<ExcludeFromCodeCoverage>]
type PackageUpgradeCommand(nuget: Tk.Nuget.INugetClient) =
    inherit AsyncCommand<PackageUpgradeCommandSettings>()

    let genMarkdownReport (context: ApplicationContext, results: ApplicationScanResults, imageUri) =
        (results.hits, imageUri) |> Markdown.generateUpgrades

    let genReports (context: ApplicationContext, results: ApplicationScanResults, imageUri) =
        let ctx =
            { ReportGenerationContext.app = context
              results = results
              reportName = "pkgchk-upgrades"
              imageUri = imageUri
              genMarkdown = genMarkdownReport
              genJson = ReportGeneration.jsonReport }

        ReportGeneration.reports ctx

    let genComment (context: ApplicationContext, (results: ApplicationScanResults), reportImg) =
        let markdown =
            (context, results, reportImg) |> genMarkdownReport |> String.joinLines

        if markdown.Length < Github.maxCommentSize then
            GithubComment.create context.github.summaryTitle markdown
        else
            GithubComment.create context.github.summaryTitle "_The report is too big for Github - Please check logs_"

    let appContext (settings: PackageUpgradeCommandSettings) =
        let context = Context.upgradesContext settings

        { context with
            options = Context.loadApplyConfig context.options }

    let dotnetContext (context: ApplicationContext) =
        { DotNetScanContext.services = context.services
          projectPath = context.options.projectPath
          includeVulnerabilities = false
          includeTransitives = false
          includeDeprecations = false
          includeDependencies = false
          includeOutdated = true }

    let consoleTable (results: ApplicationScanResults) =
        seq {
            results.hits |> Console.hitsTable

            if results.hitCounts |> List.isEmpty |> not then
                results.hitCounts |> Console.hitSummaryTable
            else
                pkgchk.Console.green "No upgrades found!" |> CliCommands.console
        }

    let results (context: ApplicationContext) (hits: seq<ScaHit>) =
        let hits = hits |> Context.filterPackages context.options |> List.ofSeq

        { ApplicationScanResults.hits = hits
          hitCounts = hits |> ScaModels.hitCountSummary |> List.ofSeq
          isGoodScan = hits |> List.isEmpty || (context.options.breakOnUpgrades |> not) }

    override _.Validate
        (context: CommandContext, settings: PackageUpgradeCommandSettings)
        : Spectre.Console.ValidationResult =
        settings.Validate()

    override _.ExecuteAsync(context, settings, cancellationToken) =
        task {
            let context = appContext settings

            if context.options.suppressBanner |> not then
                CliCommands.renderBanner nuget

            Context.trace context |> ignore

            match DotNet.restore context with
            | Choice2Of2 error -> return error |> CliCommands.returnError
            | _ ->
                let scanResults = context |> dotnetContext |> DotNet.scan

                context.services.trace "Analysing results..."
                let errors = DotNet.getErrors scanResults

                if Seq.isEmpty errors |> not then
                    return errors |> String.joinLines |> CliCommands.returnError
                else

                    let results = DotNet.getHits scanResults |> results context

                    context.services.trace "Building display..."
                    results |> consoleTable |> CliCommands.renderTables

                    let reportImg = context |> Context.reportImage results.isGoodScan

                    if context.report.reportDirectory <> "" then
                        context.services.trace "Building reports..."

                        (context, results, reportImg) |> genReports |> CliCommands.renderReportLines

                    if Context.hasGithubParameters context then
                        context.services.trace "Building Github reports..."
                        let comment = genComment (context, results, reportImg)

                        if String.isNotEmpty context.github.prId then
                            do! Github.sendPrComment context comment

                        if String.isNotEmpty context.github.commit then
                            do! Github.sendCheck context results.isGoodScan comment

                    return results.isGoodScan |> CliCommands.returnCode
        }
