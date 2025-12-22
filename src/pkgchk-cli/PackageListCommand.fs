namespace pkgchk

open System.ComponentModel
open System.Diagnostics.CodeAnalysis
open Spectre.Console.Cli

[<ExcludeFromCodeCoverage>]
type PackageListCommandSettings() =
    inherit PackageCommandSettings()

    [<CommandOption("-t|--transitive")>]
    [<Description("Toggle transitive package checks. true to include them, false to exclude.")>]
    [<DefaultValue(true)>]
    member val IncludeTransitives = true with get, set

[<ExcludeFromCodeCoverage>]
type PackageListCommand(nuget: Tk.Nuget.INugetClient) =
    inherit Command<PackageListCommandSettings>()

    let config (settings: PackageListCommandSettings) =
        match settings.ConfigFile with
        | x when x <> "" -> x |> Io.toFullPath |> Io.normalise |> Config.load
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

    override _.Execute(context, settings) =
        let trace = CliCommands.trace settings.TraceLogging
        let config = config settings

        if not config.noBanner then 
            CliCommands.renderBanner nuget

        match Sca.restore config settings.ProjectPath trace with
        | Choice2Of2 error -> error |> CliCommands.returnError
        | _ ->
            let ctx =
                { ScaCommandContext.trace = trace
                  projectPath = settings.ProjectPath
                  includeVulnerabilities = false
                  includeTransitives = config.checkTransitives
                  includeDeprecations = false
                  includeDependencies = true
                  includeOutdated = false }

            let results = Sca.scan ctx

            let errors = CliCommands.getErrors results

            if Seq.isEmpty errors |> not then
                errors |> String.joinLines |> CliCommands.returnError
            else
                trace "Analysing results..."
                let hits = ScaModels.getHits results |> Config.filterPackages config
                let hitCounts = hits |> ScaModels.hitCountSummary |> List.ofSeq

                trace "Building display..."

                let renderables =
                    seq {
                        match hits with
                        | [] -> Console.noscanHeadlineTable ()
                        | hits -> hits |> Console.hitsTable

                        if hitCounts |> List.isEmpty |> not then
                            hitCounts |> Console.hitSummaryTable
                    }

                CliCommands.renderTables renderables

                ReturnCodes.validationOk
