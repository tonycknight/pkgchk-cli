namespace pkgchk

open System.Diagnostics.CodeAnalysis
open Spectre.Console.Cli

[<ExcludeFromCodeCoverage>]
type PackageListCommand(nuget: Tk.Nuget.INugetClient) =
    inherit AsyncCommand<PackageListCommandSettings>()

    let appContext (settings: PackageListCommandSettings) =
        let context = Context.listContext settings

        { context with
            options = Context.loadApplyConfig context.options }

    let dotnetContext (context: ApplicationContext) =
        { DotNetScanContext.trace = context.services.trace
          projectPath = context.options.projectPath
          includeVulnerabilities = false
          includeTransitives = context.options.includeTransitives
          includeDeprecations = false
          includeDependencies = true
          includeOutdated = false }

    let consoleTable hits hitCounts =
        seq {
            match hits with
            | [] -> Console.noscanHeadlineTable ()
            | hits -> hits |> Console.hitsTable

            if hitCounts |> List.isEmpty |> not then
                hitCounts |> Console.hitSummaryTable
        }

    let genComment (context: ApplicationContext, hits) =
        let markdown = hits |> Markdown.generateList |> String.joinLines

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

                    let hits =
                        DotNet.getHits scanResults
                        |> Context.filterPackages context.options
                        |> List.ofSeq

                    let hitCounts = hits |> ScaModels.hitCountSummary |> List.ofSeq

                    context.services.trace "Building display..."

                    consoleTable hits hitCounts |> CliCommands.renderTables

                    if context.report.reportDirectory <> "" then
                        context.services.trace "Building reports..."

                        hits
                        |> Markdown.generateList
                        |> Io.writeFile ("pkgchk-dependencies.md" |> Io.composeFilePath context.report.reportDirectory)
                        |> CliCommands.renderReportLine

                    if Context.hasGithubParameters context then
                        context.services.trace "Building Github reports..."
                        let comment = genComment (context, hits)

                        if String.isNotEmpty context.github.prId then
                            do! Github.sendPrComment context comment

                        if String.isNotEmpty context.github.commit then
                            do! Github.sendCheck context true comment

                    return ReturnCodes.validationOk
        }
