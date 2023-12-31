namespace pkgchk

open System.ComponentModel
open System.Diagnostics.CodeAnalysis
open Spectre.Console
open Spectre.Console.Cli

[<ExcludeFromCodeCoverage>]
type PackageCheckCommandSettings() =
    inherit CommandSettings()

    [<CommandArgument(0, "[SOLUTION|PROJECT]")>]
    [<Description("The solution or project file to check.")>]
    member val ProjectPath = "" with get, set

    [<CommandOption("-t|--transitives")>]
    [<Description("Check transitive packages as well as top level packages.")>]
    member val IncludeTransitives = false with get, set

    [<CommandOption("-o|--output")>]
    [<Description("Output directory for reports.")>]
    member val OutputDirectory = "" with get, set

[<ExcludeFromCodeCoverage>]
type PackageCheckCommand() =
    inherit Command<PackageCheckCommandSettings>()

    let returnError console error =
        error |> Console.error |> Console.send console
        Console.sysError

    let genReport console outDir hits =
        let md =
            match hits with
            | [] -> Markdown.formatNoHits ()
            | hits -> hits |> Markdown.formatHits

        let reportFile = outDir |> Io.toFullPath |> Io.combine "pkgchk.md" |> Io.normalise
        md |> Io.writeFile reportFile
        reportFile |> Console.reportFileBuilt |> Console.send console

    override _.Execute(context, settings) =
        let console = Spectre.Console.AnsiConsole.Console

        use proc =
            settings.ProjectPath
            |> Io.toFullPath
            |> Sca.scanVulnerabilitiesArgs settings.IncludeTransitives
            |> Io.createProcess

        match Io.run proc with
        | Choice1Of2 json ->
            match Sca.parse json with
            | Choice1Of2 [] ->
                Console.noVulnerabilities () |> console.MarkupLine

                if settings.OutputDirectory <> "" then
                    [] |> genReport console settings.OutputDirectory

                Console.validationOk

            | Choice1Of2 hits ->
                hits |> Console.vulnerabilities |> console.MarkupLine

                if settings.OutputDirectory <> "" then
                    hits |> genReport console settings.OutputDirectory

                Console.validationFailed

            | Choice2Of2 error -> error |> returnError console
        | Choice2Of2 error -> error |> returnError console
