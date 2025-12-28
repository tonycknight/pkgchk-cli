namespace pkgchk

open System.Diagnostics.CodeAnalysis
open Spectre.Console.Cli

[<ExcludeFromCodeCoverage>]
type PackageScanCommand(nuget: Tk.Nuget.INugetClient) =
    inherit AsyncCommand<PackageScanCommandSettings>()

    let genComment (context: ApplicationContext, (results: ApplicationScanResults), imageUri) =

        let markdown =
            (results.hits, results.errorHits, results.hitCounts, context.options.severities, imageUri)
            |> Markdown.generateScan
            |> String.joinLines

        if markdown.Length < Github.maxCommentSize then
            GithubComment.create context.github.summaryTitle markdown
        else
            GithubComment.create context.github.summaryTitle "_The report is too big for Github - Please check logs_"


    let isSuccessScan (hits: ScaHit list) = hits |> List.isEmpty

    let appContext (settings: PackageScanCommandSettings) =

        let context = Context.scanContext settings

        { context with
            options = Context.loadApplyConfig context.options }

    let dotnetContext (context: ApplicationContext) =
        { DotNetScanContext.trace = context.services.trace
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
                results.errorHits |> Console.vulnerabilityHeadlineTable
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
          errorHits = errorHits
          hitCounts = errorHits |> ScaModels.hitCountSummary |> List.ofSeq
          isGoodScan = isSuccessScan errorHits }

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
                    let results = DotNet.getHits scanResults |> results context

                    context.services.trace "Building display..."

                    consoleTable context results |> CliCommands.renderTables

                    let reportImg = context |> Context.reportImage results.isGoodScan

                    if context.report.reportDirectory <> "" then
                        context.services.trace "Building reports..."

                        (results.hits, results.errorHits, results.hitCounts, context.options.severities, reportImg)
                        |> Markdown.generateScan
                        |> Io.writeFile ("pkgchk.md" |> Io.composeFilePath context.report.reportDirectory)
                        |> CliCommands.renderReportLine

                    if Context.hasGithubParameters context then
                        context.services.trace "Building Github reports..."

                        let comment = genComment (context, results, reportImg)

                        if String.isNotEmpty context.github.prId then
                            do! Github.sendPrComment context comment

                        if String.isNotEmpty context.github.commit then
                            do! Github.sendCheck context results.isGoodScan comment

                    return CliCommands.returnCode results.isGoodScan
        }
