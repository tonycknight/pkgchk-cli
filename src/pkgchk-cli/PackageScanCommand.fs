namespace pkgchk

open System.Diagnostics.CodeAnalysis
open pkgchk.reporting
open pkgchk.reporting.Markdown
open pkgchk.Markdown
open Spectre.Console.Cli

[<ExcludeFromCodeCoverage>]
type PackageScanCommand(nuget: Tk.Nuget.INugetClient) =
    inherit AsyncCommand<PackageScanCommandSettings>()

    let appContext (settings: PackageScanCommandSettings) =

        let context = Context.scanContext (nuget, settings)

        { context with
            options = Context.loadApplyConfig context.options }

    let markdown (hits, countSummary, severities, imageUri) =
        seq {
            yield! titleScan countSummary

            if String.isNotEmpty imageUri then
                yield image imageUri

            yield! formatHitCounts (severities, countSummary)
            yield! formatHits hits
            yield! footer
        }

    let consoleTables (context: ApplicationContext, results: ApplicationScanResults) =
        seq {
            results.hits |> Console.hitsTable
            let mutable headlineSet = false

            if context.options.scanVulnerabilities || context.options.scanDeprecations then
                results.hitCounts |> Console.vulnerabilityHeadlineTable
                headlineSet <- true

            if results.hitCounts |> List.isEmpty |> not then
                context.options.severities |> Console.severitySettingsTable
                results.hitCounts |> Console.hitSummaryTable

            else if (not headlineSet) then
                Console.noscanHeadlineTable ()
        }

    let render (kinds: RenderKind seq) (context: ApplicationContext, results: ApplicationScanResults) =
        let options =
            { ReportGeneratorOptions.empty with
                reportDirectory = context.report.reportDirectory
                name = "pkgchk-scan" }

        kinds
        |> Seq.map (function
            | ConsoleRender ->
                { ((context, results) |> ConsoleReporting.gen options) with
                    generate =
                        fun _ data -> consoleTables data |> Seq.map Console.toRenderable |> List.ofSeq |> Task.ofResult }
                |> ReportGeneration.gen
            | JsonFile -> results.hits |> JsonReporting.gen options |> ReportGeneration.gen
            | MarkdownFile ->
                { ((results.hits, results.hitCounts, context.options.severities, (context |> Context.reportImage results.isGoodScan))
                   |> MarkdownReporting.gen options) with
                    generate = fun _ data -> markdown data |> Task.ofResult }
                |> ReportGeneration.gen
            | _ -> ReportGenerationResult.Null |> Task.ofResult)
        |> Task.iter

    // TODO: move this to render above
    let genComment (context: ApplicationContext, (results: ApplicationScanResults), imageUri) =

        let options =
            { ReportGeneratorOptions.empty with
                name = context.github.summaryTitle }

        { ReportGeneration.data = (results.hits, results.hitCounts, context.options.severities, imageUri)
          options = options
          generate = fun _ data -> data |> markdown |> Task.ofResult
          build = GithubReporting.buildComment }
        |> ReportGeneration.gen
        |> Task.result
        |> (function
        | GithubComment c -> c
        | _ -> invalidOp "Unrecognised value")

    let dotnetContext (context: ApplicationContext) =
        { DotNetScanContext.services = context.services
          projectPath = context.options.projectPath
          includeVulnerabilities = context.options.scanVulnerabilities
          includeTransitives = context.options.scanTransitives
          includeDeprecations = context.options.scanDeprecations
          includeDependencies = false
          includeOutdated = false }

    let results (context: ApplicationContext) (hits: seq<ScaHit>) =
        let hits = hits |> Context.filterPackages context.options |> List.ofSeq

        let errorHits = hits |> ScaModels.hitsByLevels context.options.severities

        { ApplicationScanResults.hits = hits
          hitCounts = errorHits |> ScaModels.hitCountSummary |> List.ofSeq
          isGoodScan = errorHits |> List.isEmpty }
              
    override _.Validate
        (context: CommandContext, settings: PackageScanCommandSettings)
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
                    let! results = DotNet.getHits scanResults |> results context |> DotNet.enrichHits context

                    context.services.trace "Rendering..."
                                        
                    let renderResults =
                        (context, results)
                        |> render (Context.renderKinds context)
                        |> Task.result
                        |> ConsoleReporting.renderReportFiles

                    if Context.hasGithubParameters context then
                        context.services.trace "Building Github reports..."

                        let reportImg = context |> Context.reportImage results.isGoodScan
                        let comment = genComment (context, results, reportImg)

                        if String.isNotEmpty context.github.prId then
                            do! Github.sendPrComment context comment

                        if String.isNotEmpty context.github.commit && (not context.github.noCheck) then
                            do! Github.sendCheck context results.isGoodScan comment

                    return CliCommands.returnCode results.isGoodScan
        }
