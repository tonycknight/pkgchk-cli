﻿namespace pkgchk

module Markdown =

    let colourise colour value =
        $"<span style='color:{colour}'>{value}</span>"

    let formatSeverityColour value =
        value |> colourise (Rendering.severityColour value)
        
    let formatSeverity value =
        $"{Rendering.severityEmote value} {formatSeverityColour value}"

    let nugetLinkPkgVsn package version =
        $"[{package}]({Rendering.nugetLink (package, version)})"

    let nugetLinkPkgSuggestion package suggestion =
        let url = Rendering.nugetLink (package, "")
        $"[{suggestion}]({url})"

    let formatReasonColour value =
        value |> colourise (Rendering.reasonColour value)

    let formatReasons = Seq.map formatReasonColour >> String.join ", "

    let formatProject value = sprintf "## **%s**" value

    let formatSeverities severities =
        severities
        |> Seq.map formatSeverityColour
        |> List.ofSeq
        |> String.joinPretty "," "or"
        |> sprintf "__Vulnerabilities found matching %s__"

    let footer =
        seq {
            ""
            "---"
            ""
            "_Checked with :heart: by [pkgchk-cli](https://github.com/tonycknight/pkgchk-cli)_"
            ""
            "---"
        }

    let title hits =
        match hits with
        | [] -> seq { "# :heavy_check_mark: No vulnerabilities found!" }
        | _ -> seq { "# :warning: Vulnerabilities found!" }

    let formatHitCounts (severities: seq<string>, counts: seq<ScaHitSummary>) =
        let tableHdr =
            seq {
                "| Kind | Severity | Count |"
                "| - | - | - |"
            }

        let lines =
            counts
            |> Seq.map (fun hks ->
                let fmt =
                    function
                    | ScaHitKind.VulnerabilityTransitive
                    | ScaHitKind.Vulnerability -> formatSeverity
                    | ScaHitKind.Deprecated -> formatReasonColour

                $"|{Rendering.formatHitKind hks.kind}|{fmt hks.kind hks.severity}|{hks.count}|")

        if Seq.isEmpty counts then
            Seq.empty
        else
            seq {
                yield formatProject "Matching severities"
                yield formatSeverities severities
                yield! tableHdr
                yield! lines
                yield "---"
            }



    let formatHits (hits: seq<ScaHit>) =
        let grps = hits |> Seq.groupBy (fun h -> h.projectPath) |> Seq.sortBy fst

        let grpHdr =
            seq {
                "| | | | |"
                "|-|-|-|-|"
            }

        let fmt (hit: ScaHit) =
            seq {
                match hit.kind with
                | ScaHitKind.VulnerabilityTransitive
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

        (grps |> Seq.collect fmtGrp)

    let generate (hits, errorHits, countSummary, severities) =
        let title = title errorHits

        match hits with
        | [] -> Seq.append title footer
        | hits ->
            seq {
                yield! title
                yield! formatHitCounts (severities, countSummary)
                yield! formatHits hits
                yield! footer
            }
