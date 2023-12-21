namespace pkgchk

open System.ComponentModel
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
    
    override _.Execute(context, settings) =
        let r =
            Sca.createProcess settings.ProjectPath settings.IncludeTransitives |> Sca.get

        match r with
        | Choice1Of2 json ->
            match Sca.parse json with
            | Choice1Of2 [] -> Console.returnNoVulnerabilities ()
            | Choice1Of2 hits -> Console.returnVulnerabilities hits
            | Choice2Of2 error -> Console.returnError error
        | Choice2Of2 error -> Console.returnError error
