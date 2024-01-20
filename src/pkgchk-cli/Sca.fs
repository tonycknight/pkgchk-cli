namespace pkgchk

open System
open FSharp.Data

type ScaData = JsonProvider<"ScaSample.json">

type ScaHitKind =
    | Vulnerability
    | VulnerabilityTransitive
    | Deprecated

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
    
    let restoreArgs projectPath =
        projectPath |> Io.toFullPath |> sprintf "restore %s"

    let scanArgs (projectPath, includeTransitives, includeDeprecations) =
        let projPath = projectPath |> Io.toFullPath

        [| yield projPath |> ScaArgs.scanVulnerabilities includeTransitives
           if includeDeprecations then
               yield projPath |> ScaArgs.scanDeprecations includeTransitives |]

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
            Choice2Of2("An error occurred parsing results." + Environment.NewLine)

    let hitsByLevels levels (hits: ScaHit list) =
        let levels = levels |> HashSet.ofSeq StringComparer.InvariantCultureIgnoreCase

        let filter =
            (fun (h: ScaHit) ->
                match h.kind with
                | ScaHitKind.VulnerabilityTransitive
                | ScaHitKind.Vulnerability -> h.severity |> HashSet.contains levels
                | ScaHitKind.Deprecated -> h.reasons |> Seq.exists (fun r -> r |> HashSet.contains levels))

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
            |> Seq.map (fun (s, xs) -> (kind, s, xs |> Seq.length)))
