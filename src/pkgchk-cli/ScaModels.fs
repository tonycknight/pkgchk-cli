namespace pkgchk

open System

type ScaHitKind =
    | Vulnerability = 0
    | VulnerabilityTransitive = 1
    | Deprecated = 2
    | Dependency = 3
    | DependencyTransitive = 4

type NugetPackageMetadata =
    { description: string
      title: string
      summary: string
      authors: string
      tags: string
      projectUrl: string option
      license: string
      licenseUrl: string option
      readmeUrl: string option
      published: DateTimeOffset option
      requireLicenseAcceptance: bool
      totalDownloads: int64 option }

type ScaHit =
    { kind: ScaHitKind
      framework: string
      projectPath: string
      packageId: string
      resolvedVersion: string
      severity: string
      advisoryUri: string
      reasons: string[]
      metadata: NugetPackageMetadata option
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
          metadata = None
          kind = ScaHitKind.Vulnerability }

type ScaHitSummary =
    { kind: ScaHitKind
      severity: string
      count: int }

type ApplicationScanResults =
    { hits: ScaHit list
      hitCounts: ScaHitSummary list
      isGoodScan: bool }

type ReportFormat =
    | Markdown = 0
    | Json = 1

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
                | ScaHitKind.DependencyTransitive -> false
                | x -> failwith $"Unrecognised value {x}")

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

    let packageMetadata (value: Tk.Nuget.PackageMetadata) =
        { NugetPackageMetadata.authors = value.Authors
          description = value.Description
          title = value.Title
          summary = value.Summary
          tags = value.Tags
          projectUrl = value.ProjectUrl |> Option.ofNull |> Option.map _.ToString()
          license = value.License
          licenseUrl = value.LicenseUrl |> Option.ofNull |> Option.map _.ToString()
          readmeUrl = value.ReadmeUrl |> Option.ofNull |> Option.map _.ToString()
          published = value.Published |> Option.ofNullable
          requireLicenseAcceptance = value.RequireLicenseAcceptance
          totalDownloads = value.DownloadCount |> Option.ofNullable }
