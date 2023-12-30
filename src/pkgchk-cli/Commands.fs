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
            | Choice1Of2 [] -> Console.returnNoVulnerabilities console
            | Choice1Of2 hits -> hits |> Console.returnVulnerabilities console
            | Choice2Of2 error -> error |> Console.returnError console
        | Choice2Of2 error -> error |> Console.returnError console
