namespace pkgchk

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
      reasons: string []
      suggestedReplacement: string }

module ScaArgs =

    let scanArgs vulnerable includeTransitive path =
        sprintf
            "%s %s %s %s"
            (sprintf "list %s package" path)
            (match vulnerable with
             | true -> "--vulnerable"
             | false -> "--deprecated")
            (match includeTransitive with
             | true -> "--include-transitive"
             | _ -> "")
            "--format json --output-version 1"

    let scanVulnerabilities = scanArgs true

    let scanDeprecations = scanArgs false

module Sca =

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
                                  reasons = [||]
                                  suggestedReplacement = ""
                                  advisoryUri = v.Advisoryurl }))))

            let topLevelDeprecations =
                r.Projects
                |> Seq.collect (fun p ->
                    p.Frameworks
                    |> Seq.collect (fun f ->
                        f.TopLevelPackages
                        |> Seq.filter (fun tp -> tp.DeprecationReasons |> Array.isEmpty |> not)
                        |> Seq.map (fun tp ->                            
                                { ScaHit.projectPath = System.IO.Path.GetFullPath(p.Path)
                                  kind = ScaHitKind.Deprecated
                                  framework = f.Framework
                                  packageId = tp.Id
                                  resolvedVersion = tp.ResolvedVersion
                                  severity = ""
                                  suggestedReplacement =
                                    match tp.AlternativePackage with
                                    | Some ap -> sprintf "%s %s" ap.Id ap.VersionRange
                                    | None -> ""
                                  reasons = tp.DeprecationReasons |> Array.ofSeq

                                  advisoryUri = "" 
                                } )))

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
                                  reasons = [||]
                                  suggestedReplacement = ""
                                  advisoryUri = v.Advisoryurl }))))

            let hits =
                topLevelDeprecations
                |> Seq.append transitiveVuls
                |> Seq.append topLevelVuls
                |> List.ofSeq

            Choice1Of2 hits
        with ex ->
            Choice2Of2("An error occurred parsing results." + Environment.NewLine)
