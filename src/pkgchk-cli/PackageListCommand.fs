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
              severities = [||]
              breakOnVulnerabilities = false
              breakOnDeprecations = false
              checkTransitives = settings.IncludeTransitives }

    override _.Execute(context, settings) =
        let trace = Commands.trace settings.TraceLogging
        let config = config settings

        if config.noBanner |> not then
            nuget |> App.banner |> Commands.console

        match Commands.restore settings trace with
        | Choice2Of2 error -> error |> Commands.returnError
        | _ ->
            let results =
                (settings.ProjectPath, false, config.checkTransitives, false, true, false)
                |> Commands.scan trace

            let errors = Commands.getErrors results

            if Seq.isEmpty errors |> not then
                errors |> String.joinLines |> Commands.returnError
            else
                trace "Analysing results..."
                let hits = Commands.getHits results |> Config.filterPackages config
                let hitCounts = hits |> Sca.hitCountSummary |> List.ofSeq

                trace "Building display..."

                let renderables =
                    seq {
                        match hits with
                        | [] -> Console.noscanHeadlineTable ()
                        | hits -> hits |> Console.hitsTable

                        if hitCounts |> List.isEmpty |> not then
                            hitCounts |> Console.hitSummaryTable
                    }

                Commands.renderTables renderables

                ReturnCodes.validationOk
