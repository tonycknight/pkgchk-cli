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

    let nugetLinkPkgVsn package version =
        $"[{package}](https://www.nuget.org/packages/{package}/{version})"

    let nugetLinkPkgSuggestion package suggestion =
        $"[{suggestion}](https://www.nuget.org/packages/{package})"

    let formatReasons values =
        let formatReason value =
            let colour =
                function
                | "Legacy" -> "yellow"
                | "Critical Bugs" -> "red"
                | _ -> ""

            sprintf "<span style='color:%s'>%s</span>" (colour value) value

        values |> Seq.map formatReason |> String.join ", "

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
                match hit.kind with
                | ScaHitKind.Vulnerability ->
                    sprintf
                        "| %s | %s | %s %s | [Advisory](%s) | "
                        (formatHitKind hit.kind)
                        (formatSeverity hit.severity)
                        (nugetLinkPkgVsn hit.packageId hit.resolvedVersion)
                        hit.resolvedVersion
                        hit.advisoryUri
                | ScaHitKind.Deprecated ->
                    sprintf
                        "| %s | %s | %s %s | %s | "
                        (formatHitKind hit.kind)
                        (formatReasons hit.reasons)
                        (nugetLinkPkgVsn hit.packageId hit.resolvedVersion)
                        hit.resolvedVersion
                        (match (hit.suggestedReplacement, hit.alternativePackageId) with
                         | "", _ -> ""
                         | x, y when x <> "" && y <> "" -> nugetLinkPkgSuggestion y x |> sprintf "Use %s"
                         | x, _ -> x |> sprintf "Use %s")
            }

        let fmtGrp (hit: (string * seq<ScaHit>)) =
            let projectPath, hits = hit

            seq {
                projectPath |> formatProject
                ""
                yield! grpHdr

                yield!
                    hits
                    |> Seq.sortBy (fun h ->
                        ((match h.kind with
                          | ScaHitKind.Vulnerability -> 0
                          | _ -> 1),
                         h.packageId))
                    |> Seq.collect fmt
            }

        footer |> Seq.append (grps |> Seq.collect fmtGrp) |> Seq.append hdr
