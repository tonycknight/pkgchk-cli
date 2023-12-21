namespace pkgchk

open System
open System.ComponentModel
open Spectre.Console
open Spectre.Console.Cli

type PackageCheckCommandSettings() =
    inherit CommandSettings()

    [<CommandArgument(0, "[SOLUTION|PROJECT]")>]
    [<Description("The solution or project file to check.")>]
    member val ProjectPath = "" with get, set

    [<CommandOption("-t|--transitives")>]
    [<Description("Check transitive packages as well as top level packages.")>]
    member val IncludeTransitives = false with get, set

type PackageCheckCommand() =
    inherit Command<PackageCheckCommandSettings>()

    let returnNoVulnerabilities () =
        "[bold green]No vulnerabilities found![/]"
        |> AnsiConsole.Markup
        |> Console.Out.WriteLine

        0

    let returnVulnerabilities hits =
        "[bold red]Vulnerabilities found![/]"
        |> AnsiConsole.Markup
        |> Console.Out.WriteLine

        hits |> Sca.formatHits |> Console.Out.WriteLine
        1

    let returnError (error: string) =
        Console.Error.WriteLine error
        2

    override _.Execute(context, settings) =
        let r =
            Sca.createProcess settings.ProjectPath settings.IncludeTransitives |> Sca.get

        match r with
        | Choice1Of2 json ->
            match Sca.parse json with
            | Choice1Of2 [] -> returnNoVulnerabilities ()
            | Choice1Of2 hits -> returnVulnerabilities hits
            | Choice2Of2 error -> returnError error
        | Choice2Of2 error -> returnError error
