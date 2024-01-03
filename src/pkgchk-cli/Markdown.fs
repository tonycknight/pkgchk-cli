namespace pkgchk

module Markdown =

    let formatSeverity value =
        $"{Rendering.severityEmote value} <span style='color:{Rendering.severityColour value}'>{value}</span>"

    let nugetLinkPkgVsn package version =        
        $"[{package}]({Rendering.nugetLink (package, version)})"

    let nugetLinkPkgSuggestion package suggestion =
        let url = Rendering.nugetLink (package, "")
        $"[{suggestion}]({url})"
        
    let formatReasons values =
        let formatReason value =
            $"<span style='color:{Rendering.reasonColour value}'>{value}</span>"
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
                        (Rendering.formatHitKind hit.kind)
                        (formatSeverity hit.severity)
                        (nugetLinkPkgVsn hit.packageId hit.resolvedVersion)
                        hit.resolvedVersion
                        hit.advisoryUri
                | ScaHitKind.Deprecated ->
                    sprintf
                        "| %s | %s | %s %s | %s | "
                        (Rendering.formatHitKind hit.kind)
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

    let generate hits =
        match hits with
        | [] -> formatNoHits ()
        | hits -> hits |> formatHits
