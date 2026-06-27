namespace pkgchk

open System.Diagnostics.CodeAnalysis
open pkgchk.reporting
open Spectre.Console.Cli

[<ExcludeFromCodeCoverage>]
type PackageScanCommand(nuget: Tk.Nuget.INugetClient) =
    inherit AsyncCommand<PackageScanCommandSettings>()

    let appContext (settings: PackageScanCommandSettings) =

        let context = Context.scanContext (nuget, settings)

        { context with
            options = Context.loadApplyConfig context.options }                
    
    let consoleTable (context: ApplicationContext, results: ApplicationScanResults) =
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

    let genReports (kinds: ReportKind seq) (context: ApplicationContext, results: ApplicationScanResults, imageUri) =                
        let options = { ReportGeneratorOptions.reportDirectory = context.report.reportDirectory; name = "pkgchk_scan" }
        
        task {
            let mutable reportResults = []
            for kind in kinds do
                let! r = 
                    match kind with
                    | ConsoleRender ->
                        {   ReportGeneration.data = (context, results)
                            options = options
                            generate = fun _ data -> consoleTable data |> Seq.map Console.toRenderable |> List.ofSeq |> Task.ofResult
                            build = ConsoleReporting.build context.services.console }
                        |> ReportGeneration.gen
                    | JsonFile ->                             
                        {   ReportGeneration.data = results.hits
                            options = options
                            generate = JsonReporting.generate; build = JsonReporting.build }
                        |> ReportGeneration.gen
                    | MarkdownFile -> 
                        {   ReportGeneration.data = (results.hits, results.hitCounts, context.options.severities, imageUri)
                            options = options
                            generate = fun _ data -> Markdown.generateScan data |> Task.ofResult
                            build = MarkdownReporting.build }
                        |> ReportGeneration.gen
                    | _ -> ReportGenerationResult.Null |> Task.ofResult

                reportResults <- r :: reportResults
                
            return reportResults
        }
        
    // TODO: move this to genReports above
    let genComment (context: ApplicationContext, (results: ApplicationScanResults), imageUri) =
        
        let options = { ReportGeneratorOptions.empty with name = context.github.summaryTitle }
        
        {   ReportGeneration.data = (results.hits, results.hitCounts, context.options.severities, imageUri) 
            options = options
            generate = fun _ data -> data |> Markdown.generateScan |> Task.ofResult
            build = GithubReporting.buildComment }
        |> ReportGeneration.gen 
        |> Task.result
        |> (function | GithubComment c -> c | _ -> invalidOp "Unrecognised value")

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

    let reportKinds (context: ApplicationContext) =
        let kinds = [ ReportKind.ConsoleRender ]        
        if context.report.reportDirectory <> "" then kinds @ (context.report.formats |> Seq.map ScaModels.toReportKind |> List.ofSeq)
        else kinds

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

                    context.services.trace "Building display..."

                    let reportImg = context |> Context.reportImage results.isGoodScan

                    let renderResults =
                        (context, results, reportImg) 
                        |> genReports (reportKinds context)
                        |> Task.result
                        |> ConsoleReporting.renderReportFiles
                                                            
                    if Context.hasGithubParameters context then
                        context.services.trace "Building Github reports..."

                        let comment = genComment (context, results, reportImg)

                        if String.isNotEmpty context.github.prId then
                            do! Github.sendPrComment context comment

                        if String.isNotEmpty context.github.commit && (not context.github.noCheck) then
                            do! Github.sendCheck context results.isGoodScan comment

                    return CliCommands.returnCode results.isGoodScan
        }
