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

[<ExcludeFromCodeCoverage>]
type PackageCheckCommand() =
    inherit Command<PackageCheckCommandSettings>()

    let returnError console error =
        error |> Console.error console
        Console.sysError

    let returnVulnerabilities console hits =
        hits |> Console.vulnerabilities console
        Console.validationFailed

    let returnNoVulnerabilities console =
        Console.noVulnerabilities console
        Console.validationOk

    override _.Execute(context, settings) =
        let console = Spectre.Console.AnsiConsole.Console

        use proc =
            settings.ProjectPath
            |> Io.toFullPath
            |> Sca.createProcess settings.IncludeTransitives

        let r = Sca.get proc

        match r with
        | Choice1Of2 json ->
            match Sca.parse json with
            | Choice1Of2 [] -> returnNoVulnerabilities console
            | Choice1Of2 hits -> hits |> returnVulnerabilities console
            | Choice2Of2 error -> error |> returnError console
        | Choice2Of2 error -> error |> returnError console
