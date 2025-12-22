namespace pkgchk

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
