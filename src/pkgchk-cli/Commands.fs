﻿namespace pkgchk

open System
open System.ComponentModel
open System.Diagnostics.CodeAnalysis
open Spectre.Console.Cli

[<ExcludeFromCodeCoverage>]
type PackageCheckCommandSettings() =
    inherit CommandSettings()

    [<CommandArgument(0, "[SOLUTION|PROJECT]")>]
    [<Description("The solution or project file to check.")>]
    [<DefaultValue("")>]
    member val ProjectPath = "" with get, set

    [<CommandOption("-t|--transitives")>]
    [<Description("Toggle transitive package checks. -t true to include them, -t false to exclude.")>]
    [<DefaultValue(true)>]
    member val IncludeTransitives = true with get, set

    // TODO: shorts, etc.
    [<CommandOption("-u|--vulnerable")>]
    [<Description("Toggle vulnerable package checks. -t true to include them, -t false to exclude.")>]
    [<DefaultValue(true)>]
    member val IncludeVulnerabilities = true with get, set

    [<CommandOption("-d|--deprecations")>]
    [<Description("Toggle deprecated package checks. -t true to include them, -t false to exclude.")>]
    [<DefaultValue(false)>]
    member val IncludeDeprecations = true with get, set

    [<CommandOption("-o|--output")>]
    [<Description("Output directory for reports.")>]
    [<DefaultValue("")>]
    member val OutputDirectory = "" with get, set

[<ExcludeFromCodeCoverage>]
type PackageCheckCommand() =
    inherit Command<PackageCheckCommandSettings>()

    let console = Spectre.Console.AnsiConsole.Console |> Console.send

    let genArgs (settings: PackageCheckCommandSettings) =
        let projPath = settings.ProjectPath |> Io.toFullPath
                
        [|  if settings.IncludeVulnerabilities then
                yield projPath |> Sca.scanVulnerabilitiesArgs settings.IncludeTransitives
            if settings.IncludeDeprecations then
                yield projPath |> Sca.scanDeprecationsArgs settings.IncludeTransitives 
        |]
        

    let runProc proc =
        try
            Io.run proc
        finally
            proc.Dispose()

    let returnError error =
        error |> Console.error |> console
        Console.sysError

    let getErrors procResults = 
        procResults
            |> Seq.map (function
                | Choice2Of2 x -> x
                | _ -> "")
            |> Seq.filter String.isNotEmpty

    let getHits procResults =
        procResults
            |> Seq.collect (function
                | Choice1Of2 xs -> xs
                | _ -> [])
            |> List.ofSeq

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
        let procs = settings |> genArgs |> Array.map Io.createProcess

        let procResults =
            procs
            |> Array.map runProc
            |> Array.map (fun r ->
                match r with
                | Choice1Of2 json -> Sca.parse json
                | Choice2Of2 x -> Choice2Of2 x)

        let errors = getErrors procResults
        
        if Seq.isEmpty errors |> not then
            errors |> String.joinLines |> returnError
        else
            let hits = getHits procResults
            
            hits |> genConsole

            if settings.OutputDirectory <> "" then
                hits |> genReport settings.OutputDirectory |> Console.reportFileBuilt |> console

            returnCode hits
