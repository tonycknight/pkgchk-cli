namespace pkgchk

open System
open System.ComponentModel
open System.Diagnostics.CodeAnalysis
open Spectre.Console.Cli

[<ExcludeFromCodeCoverage>]
type PackageUpgradeCommandSettings() =
    inherit PackageCommandSettings()

    [<CommandOption("-o|--output")>]
    [<Description("Output directory for reports.")>]
    [<DefaultValue("")>]
    member val OutputDirectory = "" with get, set

[<ExcludeFromCodeCoverage>]
type PackageUpgradeCommand(nuget: Tk.Nuget.INugetClient) =
    inherit Command<PackageUpgradeCommandSettings>()

    override _.Execute(context, settings) =
        let trace = Commands.trace settings.TraceLogging

        if settings.NoBanner |> not then
            nuget |> App.banner |> Commands.console

        match Commands.restore settings trace with
        | Choice2Of2 error -> error |> Commands.returnError
        | _ ->
            let results =
                (settings.ProjectPath, false, false, false, false, true) |> Commands.scan trace

            let errors = Commands.getErrors results

            if Seq.isEmpty errors |> not then
                errors |> String.joinLines |> Commands.returnError
            else
                trace "Analysing results..."
                let hits = Commands.getHits results
                let hitCounts = hits |> Sca.hitCountSummary |> List.ofSeq

                trace "Building display..."

                let renderables =
                    seq {
                        hits |> Console.hitsTable

                        if hitCounts |> List.isEmpty |> not then
                            hitCounts |> Console.hitSummaryTable
                    }

                Commands.renderTables renderables
                                
                if settings.OutputDirectory <> "" then
                    trace "Building reports..."
                    let reportFile =
                        Io.toFullPath >> Io.combine "pkgchk.md" >> Io.normalise

                    let reportFile =
                        hits
                        |> Markdown.generateUpgrades
                        |> Io.writeFile (reportFile settings.OutputDirectory)

                    $"{Environment.NewLine}Report file [link={reportFile}]{reportFile}[/] built."
                    |> Console.italic
                    |> Commands.console

                ReturnCodes.validationOk
