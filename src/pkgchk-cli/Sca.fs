namespace pkgchk

open System
open System.Diagnostics
open FSharp.Data
open Spectre.Console

type ScaData = JsonProvider<"ScaSample.json">

type ScaHit =
    { framework: string
      projectPath: string
      packageId: string
      resolvedVersion: string
      severity: string
      advisoryUri: string }

module Sca =

    let createProcess includeTransitive path =
        let p = new Process()

        p.StartInfo.UseShellExecute <- false
        p.StartInfo.RedirectStandardOutput <- true
        p.StartInfo.FileName <- "dotnet"
        p.StartInfo.CreateNoWindow <- true
        p.StartInfo.WindowStyle <- ProcessWindowStyle.Hidden
        p.StartInfo.RedirectStandardError <- true
        p.StartInfo.RedirectStandardOutput <- true
        p.StartInfo.WorkingDirectory <- Environment.CurrentDirectory

        let transitives =
            match includeTransitive with
            | true -> "--include-transitive"
            | _ -> ""

        let args =
            if String.IsNullOrWhiteSpace path then
                sprintf " list package --vulnerable %s --format json --output-version 1 " transitives
            else
                sprintf " list %s package --vulnerable %s --format json --output-version 1 " path transitives

        p.StartInfo.Arguments <- args

        p


    let get (proc: Process) =
        try
            if proc.Start() then
                let out = proc.StandardOutput.ReadToEnd()
                let err = proc.StandardError.ReadToEnd()
                proc.WaitForExit()

                if (String.IsNullOrWhiteSpace(err)) then
                    Choice1Of2(out)
                else
                    Choice2Of2(err)
            else
                Choice2Of2("Cannot start process")
        with ex ->
            Choice2Of2(ex.Message)

    let parse json =

        try
            let r = ScaData.Parse(json)

            let topLevelVuls =
                r.Projects
                |> Seq.collect (fun p ->
                    p.Frameworks
                    |> Seq.collect (fun f ->
                        f.TopLevelPackages
                        |> Seq.collect (fun tp ->
                            tp.Vulnerabilities
                            |> Seq.map (fun v ->
                                { ScaHit.projectPath = p.Path
                                  framework = f.Framework
                                  packageId = tp.Id
                                  resolvedVersion = tp.ResolvedVersion
                                  severity = v.Severity
                                  advisoryUri = v.Advisoryurl }))))

            let transitiveVuls =
                r.Projects
                |> Seq.collect (fun p ->
                    p.Frameworks
                    |> Seq.collect (fun f ->
                        f.TransitivePackages
                        |> Seq.collect (fun tp ->
                            tp.Vulnerabilities
                            |> Seq.map (fun v ->
                                { ScaHit.projectPath = p.Path
                                  framework = f.Framework
                                  packageId = tp.Id
                                  resolvedVersion = tp.ResolvedVersion
                                  severity = v.Severity
                                  advisoryUri = v.Advisoryurl }))))

            let hits = topLevelVuls |> Seq.append transitiveVuls |> List.ofSeq
            Choice1Of2 hits
        with ex ->
            Choice2Of2("An error occurred parsing results" + Environment.NewLine + ex.Message)

    let formatSeverity value =
        let code =
            match value with
            | "High" -> "red"
            | "Critical" -> "italic red"
            | "Moderate" -> "#d75f00"
            | _ -> "yellow"

        sprintf "[%s]%s[/]" code value

    let formatProject value = sprintf "[bold yellow]%s[/]" value

    let formatHits (hits: seq<ScaHit>) =
        
        let fmt (hit: ScaHit) =
            seq {
                ""
                sprintf "Project:          %s" hit.projectPath |> formatProject
                sprintf "Severity:         %s" (formatSeverity hit.severity)
                sprintf "Package:          [cyan]%s[/] version [cyan]%s[/]" hit.packageId hit.resolvedVersion
                sprintf "Advisory URL:     %s" hit.advisoryUri
            }

        let lines = hits |> Seq.collect fmt
        String.Join(Environment.NewLine, lines) |> AnsiConsole.MarkupLine
