namespace pkgchk

open System

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

module ScaModels =

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
