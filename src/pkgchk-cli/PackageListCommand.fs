namespace pkgchk

open System.ComponentModel
open System.Diagnostics.CodeAnalysis
open Spectre.Console.Cli

[<ExcludeFromCodeCoverage>]
type PackageListCommandSettings() =
    inherit PackageGithubCommandSettings()

    [<CommandOption("-t|--transitive")>]
    [<Description("Toggle transitive package checks. true to include them, false to exclude.")>]
    [<DefaultValue(true)>]
    member val IncludeTransitives = true with get, set

[<ExcludeFromCodeCoverage>]
type PackageListCommand(nuget: Tk.Nuget.INugetClient) =
    inherit Command<PackageListCommandSettings>()

    let config (settings: PackageListCommandSettings) =
        match settings.ConfigFile with
        | x when x <> "" -> x |> Io.fullPath |> Io.normalise |> Config.load
        | _ ->
            { pkgchk.ScanConfiguration.includedPackages = settings.IncludedPackages
              excludedPackages = settings.ExcludedPackages
              breakOnUpgrades = false
              noBanner = settings.NoBanner
              noRestore = settings.NoRestore
              severities = [||]
              breakOnVulnerabilities = false
              breakOnDeprecations = false
              checkTransitives = settings.IncludeTransitives }

    let commandContext trace (settings: PackageListCommandSettings) config =
        { ScaCommandContext.trace = trace
          projectPath = settings.ProjectPath
          includeVulnerabilities = false
          includeTransitives = config.checkTransitives
          includeDeprecations = false
          includeDependencies = true
          includeOutdated = false }

    let renderables hits hitCounts =
        seq {
            match hits with
            | [] -> Console.noscanHeadlineTable ()
            | hits -> hits |> Console.hitsTable

            if hitCounts |> List.isEmpty |> not then
                hitCounts |> Console.hitSummaryTable
        }

    let genComment (settings: PackageListCommandSettings, hits) =
        let markdown = hits |> Markdown.generateList |> String.joinLines

        if markdown.Length < Github.maxCommentSize then
            GithubComment.create settings.GithubSummaryTitle markdown
        else
            GithubComment.create settings.GithubSummaryTitle "_The report is too big for Github - Please check logs_"

    override _.Execute(context, settings) =
        let trace = CliCommands.trace settings.TraceLogging
        let config = config settings

        if not config.noBanner then
            CliCommands.renderBanner nuget

        match DotNet.restore config settings.ProjectPath trace with
        | Choice2Of2 error -> error |> CliCommands.returnError
        | _ ->
            let ctx = commandContext trace settings config

            let results = DotNet.scan ctx

            let errors = DotNet.scanErrors results

            if Seq.isEmpty errors |> not then
                errors |> String.joinLines |> CliCommands.returnError
            else
                trace "Analysing results..."
                let hits = ScaModels.getHits results |> Config.filterPackages config
                let hitCounts = hits |> ScaModels.hitCountSummary |> List.ofSeq

                trace "Building display..."

                renderables hits hitCounts |> CliCommands.renderTables

                if settings.OutputDirectory <> "" then
                    trace "Building reports..."

                    hits
                    |> Markdown.generateList
                    |> Io.writeFile ("pkgchk-dependencies.md" |> Io.composeFilePath settings.OutputDirectory)
                    |> CliCommands.renderReportLine

                if settings.HasGithubParamters() then
                    trace "Building Github reports..."
                    let comment = genComment (settings, hits)

                    if String.isNotEmpty settings.GithubPrId then
                        Github.sendPrComment settings trace comment

                    if String.isNotEmpty settings.GithubCommit then
                        Github.sendCheck settings trace true comment

                ReturnCodes.validationOk
