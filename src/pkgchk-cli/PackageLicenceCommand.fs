namespace pkgchk

open pkgchk.Github
open pkgchk.reporting
open System.Diagnostics.CodeAnalysis
open Spectre.Console.Cli
open Tk.Nuget

[<ExcludeFromCodeCoverage>]
type PackageLicenceCommand(nuget: INugetClient) =
    inherit AsyncCommand<PackageLicenceCommandSettings>()

    let appContext (settings: PackageLicenceCommandSettings) =
        let context = Context.licenceContext (nuget, settings)

        { context with
            options = Context.loadApplyConfig context.options }
                        
    let consoleTables (results: ApplicationScanResults) =
        seq {
            match results.hits with
            | [] -> Console.noscanHeadlineTable ()
            | hits -> hits |> Console.hitsTable

            if results.hitCounts |> List.isEmpty |> not then
                results.hitCounts |> Console.hitSummaryTable
        } |> Seq.map Console.toRenderable
    
    let render (context: ApplicationContext, results: ApplicationScanResults) =
        let options =
            { ReportGeneratorOptions.empty with
                reportDirectory = context.report.reportDirectory
                trace = context.services.trace
                name = "pkgchk-licence-scan" }

        context.options.renderKinds
        |> Seq.map (function
            | ConsoleRender ->
                { (results |> ConsoleReporting.gen options) with
                    generate =
                        fun _ data ->
                            consoleTables data
                            |> List.ofSeq
                            |> Task.ofResult }
                |> ReportGeneration.gen
            | JsonFile -> results.hits |> JsonReporting.gen options |> ReportGeneration.gen
            | MarkdownFile ->
                { (results.hits |> MarkdownReporting.gen options) with
                    generate = fun _ data -> Markdown.generateList data |> Task.ofResult }
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

    let filterLicenceHits (context: ApplicationContext) (results: ApplicationScanResults) =

        let isHit (hit: ScaHit) =
            match (Context.isDisllowedLicence context.options hit, Context.isAllowedLicence context.options hit) with
            | (None, None) -> not context.options.ignoreMissingLicence
            | (Some false, None) -> false
            | (Some false, Some x) -> not x
            | (Some true, x) -> true
            | (_, Some x) -> not x

        let filteredHits = results.hits |> Seq.filter isHit |> List.ofSeq

        { ApplicationScanResults.hits = filteredHits
          hitCounts = filteredHits |> ScaModels.hitCountSummary |> List.ofSeq
          isGoodScan = filteredHits |> List.isEmpty }

    let genComment (context: ApplicationContext, results: ApplicationScanResults) =
        let markdown = results.hits |> Markdown.generateList |> String.joinLines

        if markdown.Length < Github.maxCommentSize then
            GithubComment.create context.github.summaryTitle markdown
        else
            GithubComment.create context.github.summaryTitle "_The report is too big for Github - Please check logs_"

    override _.Validate
        (context: CommandContext, settings: PackageLicenceCommandSettings)
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

                    let results = results |> filterLicenceHits context

                    context.services.trace "Rendering..."

                    let! renderResults = (context, results) |> render |> Task.map ConsoleReporting.renderReportFiles

                    if Context.hasGithubParameters context then
                        context.services.trace "Building Github reports..."
                        let comment = genComment (context, results)

                        if String.isNotEmpty context.github.prId then
                            do! Github.sendPrComment context comment

                        if String.isNotEmpty context.github.commit && (not context.github.noCheck) then
                            do! Github.sendCheck context true comment

                    return CliCommands.returnCode results.isGoodScan
        }
