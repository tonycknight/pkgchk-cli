namespace pkgchk

open System.Diagnostics.CodeAnalysis
open pkgchk.Github
open pkgchk.reporting
open Spectre.Console.Cli

[<ExcludeFromCodeCoverage>]
type PackageUpgradeCommand(nuget: Tk.Nuget.INugetClient) =
    inherit AsyncCommand<PackageUpgradeCommandSettings>()

    let appContext (settings: PackageUpgradeCommandSettings) =
        let context = Context.upgradesContext (nuget, settings)

        { context with
            options = Context.loadApplyConfig context.options }

    let consoleTables (results: ApplicationScanResults) =
        seq {
            results.hits |> Console.hitsTable

            if results.hitCounts |> List.isEmpty |> not then
                results.hitCounts |> Console.hitSummaryTable
            else
                ReportTable.singleRow (Console.green "No upgrades found!") 
                |> Console.table                
        }

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

    let render (context: ApplicationContext, results: ApplicationScanResults) =
        let options =
            { ReportGeneratorOptions.empty with
                reportDirectory = context.report.reportDirectory
                name = "pkgchk-upgrades" }

        context.options.renderKinds
        |> Seq.map (function
            | ConsoleRender ->
                { (results |> ConsoleReporting.gen options) with
                    generate =
                        fun _ data -> consoleTables data |> Seq.map Console.toRenderable |> List.ofSeq |> Task.ofResult }
                |> ReportGeneration.gen
            | JsonFile -> results.hits |> JsonReporting.gen options |> ReportGeneration.gen
            | MarkdownFile ->
                { ((results.hits, (context |> Context.reportImage results.isGoodScan))
                   |> MarkdownReporting.gen options) with
                    generate = fun _ data -> Markdown.generateUpgrades data |> Task.ofResult }
                |> ReportGeneration.gen
            | _ -> ReportGenerationResult.Null |> Task.ofResult)
        |> Task.iter

    let genComment (context: ApplicationContext, (results: ApplicationScanResults), reportImg) =
        let markdown =
            (context, results, reportImg) |> genMarkdownReport |> String.joinLines

        if markdown.Length < Github.maxCommentSize then
            GithubComment.create context.github.summaryTitle markdown
        else
            GithubComment.create context.github.summaryTitle "_The report is too big for Github - Please check logs_"

    let dotnetContext (context: ApplicationContext) =
        { DotNetScanContext.services = context.services
          projectPath = context.options.projectPath
          includeVulnerabilities = false
          includeTransitives = false
          includeDeprecations = false
          includeDependencies = false
          includeOutdated = true }

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

                    let! renderResults = (context, results) |> render |> Task.map ConsoleReporting.renderReportFiles

                    if Context.hasGithubParameters context then
                        context.services.trace "Building Github reports..."
                        let reportImg = context |> Context.reportImage results.isGoodScan
                        let comment = genComment (context, results, reportImg)

                        if String.isNotEmpty context.github.prId then
                            do! Github.sendPrComment context comment

                        if String.isNotEmpty context.github.commit && (not context.github.noCheck) then
                            do! Github.sendCheck context results.isGoodScan comment

                    return results.isGoodScan |> CliCommands.returnCode
        }
