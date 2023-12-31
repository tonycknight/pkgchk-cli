namespace pkgchk

open System

module Markdown =

    let formatHitKind (value: ScaHitKind) =
        match value with
        | ScaHitKind.Vulnerability -> "Vulnerable package"
        | _ -> ""

    let formatSeverity value =
        let code =
            match value with
            | "High" -> ":x:"
            | "Critical" -> ":x:"
            | "Moderate" -> ":exclamation:"
            | _ -> ":question:"

        sprintf "%s %s" code value

    let formatProject value = sprintf "## **%s**" value

    let formatHits (hits: seq<ScaHit>) =
        let fmt (hit: ScaHit) =
            seq {
                "---"
                hit.projectPath |> formatProject
                sprintf "%s %s" (formatSeverity hit.severity) (formatHitKind hit.kind)
                sprintf "**%s** version %s" hit.packageId hit.resolvedVersion
                hit.advisoryUri
            }

        let lines = hits |> Seq.collect fmt
        String.Join(Environment.NewLine, lines)
