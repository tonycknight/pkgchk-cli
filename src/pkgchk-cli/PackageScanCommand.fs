namespace pkgchk

open System.Diagnostics.CodeAnalysis
open pkgchk.Github
open pkgchk.reporting
open Spectre.Console.Cli

[<ExcludeFromCodeCoverage>]
type PackageScanCommand(nuget: Tk.Nuget.INugetClient) =
    inherit AsyncCommand<PackageScanCommandSettings>()
                
    let genReports (context: ApplicationContext, results: ApplicationScanResults, imageUri) =                
        let options = { ReportGeneratorOptions.reportDirectory = context.report.reportDirectory; name = "pkgchk_scan" }

        task {
            let mutable reportResults = []
            for format in context.report.formats do
                let! r = 
                    match format with
                    | ReportFormat.Json ->                             
                        {   ReportGeneration.data = results.hits
                            options = options
                            generate = JsonReporting.generate; build = JsonReporting.build }
                        |> ReportGeneration.gen
                    | ReportFormat.Markdown -> 
                        {   ReportGeneration.data = (results.hits, results.hitCounts, context.options.severities, imageUri)
                            options = options
                            generate = fun _ data -> Markdown.generateScan data |> Task.ofResult
                            build = MarkdownReporting.build }
                        |> ReportGeneration.gen
                    | _ -> invalidOp $"Unrecognised format {format}"
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
        |> ReportGeneration.gen |> Task.result


    let appContext (settings: PackageScanCommandSettings) =

        let context = Context.scanContext (nuget, settings)

        { context with
            options = Context.loadApplyConfig context.options }

    let dotnetContext (context: ApplicationContext) =
        { DotNetScanContext.services = context.services
          projectPath = context.options.projectPath
          includeVulnerabilities = context.options.scanVulnerabilities
          includeTransitives = context.options.scanTransitives
          includeDeprecations = context.options.scanDeprecations
          includeDependencies = false
          includeOutdated = false }

    let consoleTable (context: ApplicationContext) (results: ApplicationScanResults) =
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
                    let! results = DotNet.getHits scanResults |> results context |> DotNet.enrichHits context

                    context.services.trace "Building display..."

                    consoleTable context results |> CliCommands.renderTables

                    let reportImg = context |> Context.reportImage results.isGoodScan

                    if context.report.reportDirectory <> "" then
                        context.services.trace "Building reports..."

                        (context, results, reportImg) 
                        |> genReports 
                        |> Task.result 
                        |> List.map (function | OutputFile path -> path | _ -> "" ) 
                        |> List.filter (fun s -> s <> "")
                        |> CliCommands.renderReportLines

                    if Context.hasGithubParameters context then
                        context.services.trace "Building Github reports..."

                        let comment = genComment (context, results, reportImg)

                        if String.isNotEmpty context.github.prId then
                            do! Github.sendPrComment context comment

                        if String.isNotEmpty context.github.commit && (not context.github.noCheck) then
                            do! Github.sendCheck context results.isGoodScan comment

                    return CliCommands.returnCode results.isGoodScan
        }
