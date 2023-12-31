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
        let grps = hits |> Seq.groupBy (fun h -> h.projectPath)
        let hdr = seq { "# :heavy_check_mark: Vulnerabilities found!" }

        let grpHdr =
            seq {
                "| | | | |"
                "|-|-|-|-|"
            }

        let fmt (hit: ScaHit) =
            seq {
                sprintf
                    "| %s | %s | %s - %s | [Advisory](%s) | "
                    (formatHitKind hit.kind)
                    (formatSeverity hit.severity)
                    hit.packageId
                    hit.resolvedVersion
                    hit.advisoryUri
            }

        let fmtGrp (hit: (string * seq<ScaHit>)) =
            let projectPath, hits = hit

            seq {
                projectPath |> formatProject
                ""
                yield! grpHdr
                yield! hits |> Seq.collect fmt
            //"---"
            }

        footer |> Seq.append (grps |> Seq.collect fmtGrp) |> Seq.append hdr
