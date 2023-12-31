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

    let footer =
        seq {
            ""
            "---"
            ""
            "_Checked with :two_hearts: by [pkgchk](https://github.com/tonycknight/pkgchk-cli)_"
            ""
            "---"
        }

    let formatNoHits () =
        let content = seq { "# :heavy_check_mark: No vulnerabilities found!" }

        footer |> Seq.append content

    let formatHits (hits: seq<ScaHit>) =
        let fmt (hit: ScaHit) =
            seq {
                hit.projectPath |> formatProject
                ""
                sprintf "%s - %s" (formatHitKind hit.kind) (formatSeverity hit.severity)
                ""
                sprintf "**%s** version %s" hit.packageId hit.resolvedVersion
                sprintf "[Advisory](%s)" hit.advisoryUri
                ""
                "---"
            }

        let hdr = seq { "# :heavy_check_mark: Vulnerabilities found!" }

        footer |> Seq.append (hits |> Seq.collect fmt) |> Seq.append hdr
