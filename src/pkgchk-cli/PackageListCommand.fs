namespace pkgchk

open System.Diagnostics.CodeAnalysis
open System.Collections.Generic
open Spectre.Console.Cli
open Tk.Nuget

[<ExcludeFromCodeCoverage>]
type PackageListCommand(nuget: INugetClient) =
    inherit AsyncCommand<PackageListCommandSettings>()

    let genMarkdownReport (context: ApplicationContext, results: ApplicationScanResults, imageUri) =
        results.hits |> Markdown.generateList

    let genReports (context: ApplicationContext, results: ApplicationScanResults, imageUri) =
        let ctx =
            { ReportGenerationContext.app = context
              results = results
              reportName = "pkgchk-dependencies"
              imageUri = imageUri
              genMarkdown = genMarkdownReport
              genJson = ReportGeneration.jsonReport }

        ReportGeneration.reports ctx

    let appContext (settings: PackageListCommandSettings) =
        let context = Context.listContext (nuget, settings)

        { context with
            options = Context.loadApplyConfig context.options }

    let dotnetContext (context: ApplicationContext) =
        { DotNetScanContext.services = context.services
          projectPath = context.options.projectPath
          includeVulnerabilities = false
          includeTransitives = context.options.scanTransitives
          includeDeprecations = false
          includeDependencies = true
          includeOutdated = false }

    let results (context: ApplicationContext) (hits: seq<ScaHit>) =
        let hits = hits |> Context.filterPackages context.options |> List.ofSeq

        { ApplicationScanResults.hits = hits
          hitCounts = hits |> ScaModels.hitCountSummary |> List.ofSeq
          isGoodScan = true }

    let enrichHits context (results: ApplicationScanResults) =
        task {
            if context.options.fetchMetadata then
                context.services.trace "Fetching package metadata..."
                let! hits = ScaModels.enrichMetadata context.services.nuget results.hits

                return { results with hits = hits }
            else
                return results
        }

    let consoleTable (results: ApplicationScanResults) =
        seq {
            match results.hits with
            | [] -> Console.noscanHeadlineTable ()
            | hits -> hits |> Console.hitsTable

            if results.hitCounts |> List.isEmpty |> not then
                results.hitCounts |> Console.hitSummaryTable
        }

    let genComment (context: ApplicationContext, results: ApplicationScanResults) =
        let markdown = (context, results, "") |> genMarkdownReport |> String.joinLines

        if markdown.Length < Github.maxCommentSize then
            GithubComment.create context.github.summaryTitle markdown
        else
            GithubComment.create context.github.summaryTitle "_The report is too big for Github - Please check logs_"

    override _.Validate
        (context: CommandContext, settings: PackageListCommandSettings)
        : Spectre.Console.ValidationResult =
        settings.Validate()

    override _.ExecuteAsync(context, settings, cancellationToken) =
        task {
            let context = settings |> appContext

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
                    let! results = scanResults |> DotNet.getHits |> results context |> enrichHits context

                    context.services.trace "Building display..."

                    results |> consoleTable |> CliCommands.renderTables

                    if context.report.reportDirectory <> "" then
                        context.services.trace "Building reports..."

                        (context, results, "") |> genReports |> CliCommands.renderReportLines

                    if Context.hasGithubParameters context then
                        context.services.trace "Building Github reports..."
                        let comment = genComment (context, results)

                        if String.isNotEmpty context.github.prId then
                            do! Github.sendPrComment context comment

                        if String.isNotEmpty context.github.commit && (not context.github.noCheck) then
                            do! Github.sendCheck context true comment

                    return ReturnCodes.validationOk
        }
