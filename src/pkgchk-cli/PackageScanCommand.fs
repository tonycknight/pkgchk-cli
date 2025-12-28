namespace pkgchk

open System.Diagnostics.CodeAnalysis
open Spectre.Console.Cli

[<ExcludeFromCodeCoverage>]
type PackageScanCommand(nuget: Tk.Nuget.INugetClient) =
    inherit AsyncCommand<PackageScanCommandSettings>()

    let rec genComment trace (settings: PackageScanCommandSettings, hits, errorHits, hitCounts, imageUri) attempt =
        let markdown =
            (hits, errorHits, hitCounts, settings.SeverityLevels, imageUri)
            |> Markdown.generateScan
            |> String.joinLines

        let summaryTitle = settings.GithubSummaryTitle

        if markdown.Length < Github.maxCommentSize then
            GithubComment.create summaryTitle markdown
        else
            trace $"Shrinking Github output as too large (attempt #{attempt + 1})..."

            if attempt >= 1 then
                GithubComment.create summaryTitle "_The report is too big for Github - Please check logs_"
            else
                genComment trace (settings, [], errorHits, hitCounts, imageUri) (attempt + 1)

    let isSuccessScan (hits: ScaHit list) = hits |> List.isEmpty
        
    let appContext (settings: PackageScanCommandSettings) = 
        
        let context = Context.scanContext settings

        { context with options = Context.loadApplyConfig context.options }

    let commandContext trace (context: ApplicationContext) =
        { ScaCommandContext.trace = trace
          projectPath = context.options.projectPath
          includeVulnerabilities = context.options.breakOnVulnerabilities
          includeTransitives = context.options.includeTransitives
          includeDeprecations = context.options.breakOnDeprecations
          includeDependencies = false
          includeOutdated = false }

    let renderables (context: ApplicationContext) hits hitCounts errorHits =
        seq {
            hits |> Console.hitsTable
            let mutable headlineSet = false

            if
                context.options.breakOnVulnerabilities
                || context.options.breakOnDeprecations
            then
                errorHits |> Console.vulnerabilityHeadlineTable
                headlineSet <- true

            if hitCounts |> List.isEmpty |> not then
                context.options.severities |> Console.severitySettingsTable
                hitCounts |> Console.hitSummaryTable

            else if (not headlineSet) then
                Console.noscanHeadlineTable ()
        }

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
                let results = context |> commandContext context.services.trace |> DotNet.scan

                context.services.trace "Analysing results..."
                let errors = DotNet.scanErrors results
                let hits = ScaModels.getHits results |> Context.filterPackages context.options |> List.ofSeq
                let errorHits = hits |> ScaModels.hitsByLevels context.options.severities
                let hitCounts = errorHits |> ScaModels.hitCountSummary |> List.ofSeq
                let isSuccess = isSuccessScan errorHits

                if Seq.isEmpty errors |> not then
                    return errors |> String.joinLines |> CliCommands.returnError
                else
                    context.services.trace "Building display..."

                    renderables context hits hitCounts errorHits |> CliCommands.renderTables

                    let reportImg =
                        match isSuccess with
                        | true -> context.report.goodImageUri
                        | false -> context.report.badImageUri

                    if context.report.reportDirectory <> "" then
                        context.services.trace "Building reports..."

                        (hits, errorHits, hitCounts, context.options.severities, reportImg)
                        |> Markdown.generateScan
                        |> Io.writeFile ("pkgchk.md" |> Io.composeFilePath context.report.reportDirectory)
                        |> CliCommands.renderReportLine

                    if settings.HasGithubParamters() then
                        context.services.trace "Building Github reports..."
                        let comment = genComment context.services.trace (settings, hits, errorHits, hitCounts, reportImg) 0

                        if String.isNotEmpty context.github.prId then
                            do! Github.sendPrComment settings context.services.trace comment

                        if String.isNotEmpty context.github.commit then
                            do! Github.sendCheck settings context.services.trace isSuccess comment

                    return CliCommands.returnCode isSuccess
        }
