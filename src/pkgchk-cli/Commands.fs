﻿namespace pkgchk

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

    let returnError console error =
        error |> Console.error console
        Console.sysError

    let reportPath outputDirectory =
        let path = Io.toFullPath outputDirectory
        let fn = "pkgchk.md"
        System.IO.Path.Combine(path, fn)

    override _.Execute(context, settings) =
        let console = Spectre.Console.AnsiConsole.Console

        use proc =
            settings.ProjectPath
            |> Io.toFullPath
            |> Sca.commandArgs settings.IncludeTransitives
            |> Io.createProcess

        match Io.run proc with
        | Choice1Of2 json ->
            match Sca.parse json with
            | Choice1Of2 [] ->
                Console.noVulnerabilities console

                if settings.OutputDirectory <> "" then
                    let reportFile = reportPath settings.OutputDirectory
                    Markdown.formatNoHits () |> Io.writeFile reportFile

                Console.validationOk
            | Choice1Of2 hits ->
                hits |> Console.vulnerabilities console

                if settings.OutputDirectory <> "" then
                    let reportFile = reportPath settings.OutputDirectory
                    hits |> Markdown.formatHits |> Io.writeFile reportFile

                Console.validationFailed
            | Choice2Of2 error -> error |> returnError console
        | Choice2Of2 error -> error |> returnError console
