﻿namespace pkgchk

open System
open FSharp.Data

type ScaData = JsonProvider<"ScaSample.json">

type ScaHitKind =
    | Vulnerability
    | Deprecated

type ScaHit =
    { kind: ScaHitKind
      framework: string
      projectPath: string
      packageId: string
      resolvedVersion: string
      severity: string
      advisoryUri: string
      commentary: string }

module Sca =

    let scanVulnerabilitiesArgs includeTransitive path =
        let transitives =
            match includeTransitive with
            | true -> "--include-transitive"
            | _ -> ""

        if String.IsNullOrWhiteSpace path then
            sprintf " list package --vulnerable %s --format json --output-version 1 " transitives
        else
            sprintf " list %s package --vulnerable %s --format json --output-version 1 " path transitives

    let scanDeprecationsArgs includeTransitive path =
        let transitives =
            match includeTransitive with
            | true -> "--include-transitive"
            | _ -> ""

        if String.IsNullOrWhiteSpace path then
            sprintf " list package --deprecated %s --format json --output-version 1 " transitives
        else
            sprintf " list %s package --deprecated %s --format json --output-version 1 " path transitives

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
                                { ScaHit.projectPath = System.IO.Path.GetFullPath(p.Path)
                                  kind = ScaHitKind.Vulnerability
                                  framework = f.Framework
                                  packageId = tp.Id
                                  resolvedVersion = tp.ResolvedVersion
                                  severity = v.Severity
                                  commentary = ""
                                  advisoryUri = v.Advisoryurl }))))

            let topLevelDeprecations =
                r.Projects
                |> Seq.collect (fun p ->
                    p.Frameworks
                    |> Seq.collect (fun f ->
                        f.TopLevelPackages
                        |> Seq.collect (fun tp ->
                            tp.DeprecationReasons
                            |> Seq.filter String.isNotEmpty
                            |> Seq.map (fun d ->
                                { ScaHit.projectPath = System.IO.Path.GetFullPath(p.Path)
                                  kind = ScaHitKind.Deprecated
                                  framework = f.Framework
                                  packageId = tp.Id
                                  resolvedVersion = tp.ResolvedVersion
                                  severity = ""
                                  commentary =
                                    match tp.AlternativePackage with
                                    | Some ap -> sprintf "Reason: %s Use %s %s" d ap.Id ap.VersionRange
                                    | None -> sprintf "Reason: %s" d

                                  advisoryUri = "" }))))

            let transitiveVuls =
                r.Projects
                |> Seq.collect (fun p ->
                    p.Frameworks
                    |> Seq.collect (fun f ->
                        f.TransitivePackages
                        |> Seq.collect (fun tp ->
                            tp.Vulnerabilities
                            |> Seq.map (fun v ->
                                { ScaHit.projectPath = System.IO.Path.GetFullPath(p.Path)
                                  kind = ScaHitKind.Vulnerability
                                  framework = f.Framework
                                  packageId = tp.Id
                                  resolvedVersion = tp.ResolvedVersion
                                  severity = v.Severity
                                  commentary = ""
                                  advisoryUri = v.Advisoryurl }))))

            let hits =
                topLevelDeprecations
                |> Seq.append transitiveVuls
                |> Seq.append topLevelVuls
                |> List.ofSeq

            Choice1Of2 hits
        with ex ->
            Choice2Of2("An error occurred parsing results." + Environment.NewLine)
