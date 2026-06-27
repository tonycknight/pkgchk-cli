namespace pkgchk

open System.Diagnostics.CodeAnalysis
open pkgchk.Markdown
open pkgchk.Github
open pkgchk.reporting
open Spectre.Console.Cli
open Tk.Nuget

[<ExcludeFromCodeCoverage>]
type PackageListCommand(nuget: INugetClient) =
    inherit AsyncCommand<PackageListCommandSettings>()

    let appContext (settings: PackageListCommandSettings) =
        let context = Context.listContext (nuget, settings)

        { context with
            options = Context.loadApplyConfig context.options }

    let consoleTables (results: ApplicationScanResults) =
        seq {
            match results.hits with
            | [] -> Console.noscanHeadlineTable ()
            | hits -> hits |> Console.hitsTable

            if results.hitCounts |> List.isEmpty |> not then
                results.hitCounts |> Console.hitSummaryTable
        }

    let markdown hits =
        seq {
            yield! titleList ()
            yield! formatHits hits
            yield! footer
        }

    let render (context: ApplicationContext, results: ApplicationScanResults) =
        let options =
            { ReportGeneratorOptions.empty with
                reportDirectory = context.report.reportDirectory
                trace = context.services.trace
                name = "pkgchk-dependencies" }

        context.options.renderKinds
        |> Seq.map (function
            | ConsoleRender ->
                { (results |> ConsoleReporting.gen options) with
                    generate =
                        fun _ data ->
                            consoleTables data
                            |> Seq.map Console.toRenderable
                            |> List.ofSeq
                            |> Task.ofResult }
                |> ReportGeneration.gen
            | JsonFile -> results.hits |> JsonReporting.gen options |> ReportGeneration.gen
            | MarkdownFile ->
                { (results.hits |> MarkdownReporting.gen options) with
                    generate = fun _ data -> markdown data |> Task.ofResult }
                |> ReportGeneration.gen
            | _ -> ReportGenerationResult.Null |> Task.ofResult)
        |> Task.iter

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

    let genComment (context: ApplicationContext, results: ApplicationScanResults) =
        let markdown = results.hits |> markdown |> String.joinLines

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

            match DotNet.restore context with
            | Choice2Of2 error -> return error |> CliCommands.returnError
            | _ ->
                let scanResults = context |> dotnetContext |> DotNet.scan

                context.services.trace "Analysing results..."
                let errors = DotNet.getErrors scanResults

                if Seq.isEmpty errors |> not then
                    return errors |> String.joinLines |> CliCommands.returnError
                else
                    let! results = scanResults |> DotNet.getHits |> results context |> DotNet.enrichHits context

                    context.services.trace "Building display..."

                    let! renderResults = (context, results) |> render |> Task.map ConsoleReporting.renderReportFiles

                    if Context.hasGithubParameters context then
                        context.services.trace "Building Github reports..."
                        let comment = genComment (context, results)

                        if String.isNotEmpty context.github.prId then
                            do! Github.sendPrComment context comment

                        if String.isNotEmpty context.github.commit && (not context.github.noCheck) then
                            do! Github.sendCheck context true comment

                    return ReturnCodes.validationOk
        }
