namespace pkgchk

open System.ComponentModel
open System.Diagnostics.CodeAnalysis
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

    let console = Spectre.Console.AnsiConsole.Console |> Console.send

    let returnError error =
        error |> Console.error |> console
        Console.sysError

    let returnCode =
        function
        | [] -> Console.validationOk
        | _ -> Console.validationFailed

    let genConsole =
        function
        | [] -> Console.noVulnerabilities () |> console
        | hits -> hits |> Console.vulnerabilities |> console

    let genMarkdown =
        function
        | [] -> Markdown.formatNoHits ()
        | hits -> hits |> Markdown.formatHits

    let genReport outDir hits =
        let reportFile = outDir |> Io.toFullPath |> Io.combine "pkgchk.md" |> Io.normalise
        hits |> genMarkdown |> Io.writeFile reportFile
        reportFile


    override _.Execute(context, settings) =

        use proc =
            settings.ProjectPath
            |> Io.toFullPath
            |> Sca.scanVulnerabilitiesArgs settings.IncludeTransitives
            |> Io.createProcess

        match Io.run proc with
        | Choice1Of2 json ->
            match Sca.parse json with
            | Choice1Of2 hits ->
                genConsole hits

                if settings.OutputDirectory <> "" then
                    hits |> genReport settings.OutputDirectory |> Console.reportFileBuilt |> console

                returnCode hits

            | Choice2Of2 error -> error |> returnError
        | Choice2Of2 error -> error |> returnError
