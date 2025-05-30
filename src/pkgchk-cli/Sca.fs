﻿namespace pkgchk

open System
open FSharp.Data

type ScaVulnerabilityData = JsonProvider<"ScaVulnerabilitySample.json">
type ScaPackageTreeData = JsonProvider<"PackageDependencyTreeSample.json">

type ScaHitKind =
    | Vulnerability
    | VulnerabilityTransitive
    | Deprecated
    | Dependency
    | DependencyTransitive

type ScaHit =
    { kind: ScaHitKind
      framework: string
      projectPath: string
      packageId: string
      resolvedVersion: string
      severity: string
      advisoryUri: string
      reasons: string[]
      suggestedReplacement: string
      alternativePackageId: string }

    static member empty =
        { ScaHit.projectPath = ""
          framework = ""
          packageId = ""
          resolvedVersion = ""
          severity = ""
          reasons = [||]
          suggestedReplacement = ""
          alternativePackageId = ""
          advisoryUri = ""
          kind = ScaHitKind.Vulnerability }

type ScaHitSummary =
    { kind: ScaHitKind
      severity: string
      count: int }

module ScaArgs =

    let private scanArgs vulnerable deprecated outdated includeTransitive path =
        sprintf
            "%s %s %s %s"
            (sprintf "list %s package" path)
            (match (vulnerable, deprecated, outdated) with
             | (true, _, _) -> "--vulnerable"
             | (_, true, _) -> "--deprecated"
             | (_, _, true) -> "--outdated"
             | (false, false, false) -> "")
            (match includeTransitive with
             | true -> "--include-transitive"
             | _ -> "")
            "--format json --output-version 1"

    let scanVulnerabilities = scanArgs true false false

    let scanDeprecations = scanArgs false true false

    let scanDependencies = scanArgs false false false

    let scanOutdated = scanArgs false false true false

module Sca =

    let restoreArgs projectPath =
        projectPath |> Io.toFullPath |> sprintf "restore %s"

    let parseError (parseable: string) (ex: Exception) =
        match
            parseable.Split(Environment.NewLine)
            |> Seq.filter (fun s -> s.StartsWith("error:"))
            |> Array.ofSeq
        with
        | [||] -> ex.Message
        | xs -> xs |> String.join Environment.NewLine

    let parseVulnerabilities json =

        try
            let r = ScaVulnerabilityData.Parse(json)

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
                                  alternativePackageId = ""
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
                              alternativePackageId =
                                match tp.AlternativePackage with
                                | Some ap -> ap.Id
                                | None -> ""
                              reasons = tp.DeprecationReasons |> Array.ofSeq

                              advisoryUri = "" })))

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
                                  kind = ScaHitKind.VulnerabilityTransitive
                                  framework = f.Framework
                                  packageId = tp.Id
                                  resolvedVersion = tp.ResolvedVersion
                                  severity = v.Severity
                                  reasons = [||]
                                  suggestedReplacement = ""
                                  alternativePackageId = ""
                                  advisoryUri = v.Advisoryurl }))))

            let hits =
                topLevelDeprecations
                |> Seq.append transitiveVuls
                |> Seq.append topLevelVuls
                |> List.ofSeq

            Choice1Of2 hits
        with ex ->
            parseError json ex |> Choice2Of2

    let parsePackageTree json =

        try
            let r = ScaPackageTreeData.Parse(json)

            let topLevelVuls =
                r.Projects
                |> Seq.collect (fun p ->
                    p.Frameworks
                    |> Seq.collect (fun f ->
                        f.TopLevelPackages
                        |> Seq.map (fun tp ->

                            { ScaHit.projectPath = System.IO.Path.GetFullPath(p.Path)
                              kind = ScaHitKind.Dependency
                              framework = f.Framework
                              packageId = tp.Id
                              resolvedVersion = tp.ResolvedVersion
                              severity = ""
                              reasons =
                                match tp.LatestVersion with
                                | Some _ -> [| "Upgrade available" |]
                                | _ -> [||]
                              suggestedReplacement = tp.LatestVersion |> Option.defaultValue ""
                              alternativePackageId = ""
                              advisoryUri = "" })))

            let transitiveVuls =
                r.Projects
                |> Seq.collect (fun p ->
                    p.Frameworks
                    |> Seq.collect (fun f ->
                        f.TransitivePackages
                        |> Seq.map (fun tp ->
                            { ScaHit.projectPath = System.IO.Path.GetFullPath(p.Path)
                              kind = ScaHitKind.DependencyTransitive
                              framework = f.Framework
                              packageId = tp.Id
                              resolvedVersion = tp.ResolvedVersion
                              severity = ""
                              reasons = [||]
                              suggestedReplacement = ""
                              alternativePackageId = ""
                              advisoryUri = "" })))

            let hits = transitiveVuls |> Seq.append topLevelVuls |> List.ofSeq

            Choice1Of2 hits
        with ex ->
            Choice2Of2("An error occurred parsing results." + Environment.NewLine)

    let scanArgs
        (
            projectPath,
            includeVulnerabilities,
            includeTransitives,
            includeDeprecations,
            includeDependencies,
            includeOutdated
        ) =
        let projPath = projectPath |> Io.toFullPath

        [| if includeVulnerabilities then
               yield (projPath |> ScaArgs.scanVulnerabilities includeTransitives, parseVulnerabilities)
           if includeDeprecations then
               yield (projPath |> ScaArgs.scanDeprecations includeTransitives, parseVulnerabilities)
           if includeDependencies then
               yield (projPath |> ScaArgs.scanDependencies includeTransitives, parsePackageTree)
           if includeOutdated then
               yield (ScaArgs.scanOutdated projPath, parsePackageTree) |]


    let hitsByLevels levels (hits: ScaHit list) =
        let levels = levels |> HashSet.ofSeq StringComparer.InvariantCultureIgnoreCase

        let filter =
            (fun (h: ScaHit) ->
                match h.kind with
                | ScaHitKind.VulnerabilityTransitive
                | ScaHitKind.Vulnerability -> h.severity |> HashSet.contains levels
                | ScaHitKind.Deprecated -> h.reasons |> Seq.exists (HashSet.contains levels)
                | ScaHitKind.Dependency
                | ScaHitKind.DependencyTransitive -> false)

        let remap (hit: ScaHit) =
            match hit.kind with
            | ScaHitKind.Deprecated ->
                let reasons = hit.reasons |> Array.filter (HashSet.contains levels)
                { hit with reasons = reasons }
            | _ -> hit

        hits |> List.filter filter |> List.map remap

    let hitCountSummary (hits: seq<ScaHit>) =
        hits
        |> Seq.groupBy (fun h -> h.kind)
        |> Seq.collect (fun (kind, hs) ->
            hs
            |> Seq.collect (fun h ->
                seq {
                    h.severity
                    yield! h.reasons
                }
                |> Seq.filter String.isNotEmpty)
            |> Seq.groupBy id
            |> Seq.map (fun (s, xs) ->
                { ScaHitSummary.kind = kind
                  severity = s
                  count = xs |> Seq.length }))
