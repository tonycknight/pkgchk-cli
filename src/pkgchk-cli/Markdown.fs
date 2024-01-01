namespace pkgchk

open System

module Markdown =

    let formatHitKind (value: ScaHitKind) =
        match value with
        | ScaHitKind.Vulnerability -> "Vulnerable package"
        | ScaHitKind.Deprecated -> "Deprecated package"

    let formatSeverity value =
        let (emote, colour) =
            match value with
            | "High" -> (":bangbang:", "red")
            | "Critical" -> (":heavy_exclamation_mark:", "red")
            | "Moderate" -> (":heavy_exclamation_mark:", "orange")
            | "" -> ("", "")
            | _ -> (":heavy_exclamation_mark:", "yellow")

        sprintf "%s <span style='color:%s'>%s</span>" emote colour value

    let formatProject value = sprintf "## **%s**" value

    let footer =
        seq {
            ""
            "---"
            ""
            "_Checked with :heart: by [pkgchk](https://github.com/tonycknight/pkgchk-cli)_"
            ""
            "---"
        }

    let formatNoHits () =
        let content = seq { "# :heavy_check_mark: No vulnerabilities found!" }

        footer |> Seq.append content

    let formatHits (hits: seq<ScaHit>) =
        let grps = hits |> Seq.groupBy (fun h -> h.projectPath) |> Seq.sortBy fst
        let hdr = seq { "# :warning: Vulnerabilities found!" }

        let grpHdr =
            seq {
                "| | | | |"
                "|-|-|-|-|"
            }

        let fmt (hit: ScaHit) =
            seq {
                sprintf
                    "| %s | %s | %s %s | [Advisory](%s) | "
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
                yield! hits |> Seq.sortBy (fun h -> h.packageId) |> Seq.collect fmt
            }

        footer |> Seq.append (grps |> Seq.collect fmtGrp) |> Seq.append hdr
