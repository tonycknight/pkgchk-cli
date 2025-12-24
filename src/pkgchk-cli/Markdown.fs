namespace pkgchk

module Markdown =

    let colourise colour value =
        $"<span style='color:{colour}'>{value}</span>"

    let formatSeverityColour value =
        value |> colourise (Rendering.severityColour value)

    let formatSeverity value =
        $"{Rendering.severityEmote value} {formatSeverityColour value}"

    let imgLink uri = $"![image]({uri})"

    let nugetLinkPkgVsn package version =
        $"[{package}]({Rendering.nugetLink (package, version)})"

    let nugetLinkPkgSuggestion package suggestion =
        let url = Rendering.nugetLink (package, "")
        $"[{suggestion}]({url})"

    let pkgFramework (hit: ScaHit) =
        hit.framework |> colourise Rendering.cornflowerblue

    let formatReasonColour value =
        value |> colourise (Rendering.reasonColour value)

    let formatReasons = Seq.map formatReasonColour >> String.join ", "

    let formatProject value = sprintf "## **%s**" value

    let formatSeverities severities =
        severities
        |> Seq.map formatSeverityColour
        |> List.ofSeq
        |> String.joinPretty ", " " or "
        |> sprintf "__Vulnerabilities found matching %s__"

    let footer =
        let now = System.DateTime.UtcNow.ToString("F")

        seq {
            ""
            "---"
            ""
            $"_Built on {now} UTC with :heart: from [{App.packageId.ToLower()}]({App.repo}) Thank you for using my software._"
            ""
            "---"
        }

    let titleScan hits =
        match hits with
        | [] -> seq { "# :heavy_check_mark: No vulnerabilities found!" }
        | _ -> seq { "# :warning: Vulnerabilities found!" }

    let titleUpgrades hits =
        match hits with
        | [] -> seq { "# :heavy_check_mark: No upgrades found!" }
        | _ -> seq { "# :warning: Upgrades found!" }

    let titleList () =
        seq { "# :heavy_check_mark: Package dependencies" }

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
                    | ScaHitKind.Dependency
                    | ScaHitKind.DependencyTransitive -> id

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

    let formatHit (hit: ScaHit) =
        seq {
            match hit.kind with
            | ScaHitKind.VulnerabilityTransitive
            | ScaHitKind.Vulnerability ->
                sprintf
                    "| %s | %s | %s: %s %s | [Advisory](%s) | "
                    (Rendering.formatHitKind hit.kind)
                    (formatSeverity hit.severity)
                    (pkgFramework hit)
                    (nugetLinkPkgVsn hit.packageId hit.resolvedVersion)
                    hit.resolvedVersion
                    hit.advisoryUri
            | ScaHitKind.Deprecated ->
                sprintf
                    "| %s | %s | %s: %s %s | %s | "
                    (Rendering.formatHitKind hit.kind)
                    (formatReasons hit.reasons)
                    (pkgFramework hit)
                    (nugetLinkPkgVsn hit.packageId hit.resolvedVersion)
                    hit.resolvedVersion
                    (match (hit.suggestedReplacement, hit.alternativePackageId) with
                     | "", _ -> ""
                     | x, y when x <> "" && y <> "" -> nugetLinkPkgSuggestion y x |> sprintf "Use %s"
                     | x, _ -> x |> sprintf "Use %s")
            | ScaHitKind.Dependency
            | ScaHitKind.DependencyTransitive ->
                sprintf
                    "| %s |  | %s: %s %s |  | "
                    (Rendering.formatHitKind hit.kind)
                    (pkgFramework hit)
                    (nugetLinkPkgVsn hit.packageId hit.resolvedVersion)
                    hit.resolvedVersion
        }

    let formatHitGroup (hit: (string * seq<ScaHit>)) =
        let grpHdr =
            seq {
                "| Kind | Severity | Package | Info |"
                "|-|-|-|-|"
            }

        let projectPath, hits = hit

        seq {
            projectPath |> formatProject
            ""
            yield! grpHdr
            yield! hits |> Seq.collect formatHit
        }

    let formatHits (hits: seq<ScaHit>) =
        hits
        |> Seq.groupBy (fun h -> h.projectPath)
        |> Seq.sortBy fst
        |> Seq.collect formatHitGroup

    let generateScan (hits, errorHits, countSummary, severities, imageUri) =
        seq {
            yield! titleScan errorHits

            if String.isNotEmpty imageUri then
                yield imgLink imageUri

            yield! formatHitCounts (severities, countSummary)
            yield! formatHits hits
            yield! footer
        }

    let generateUpgrades (hits, imageUri) =
        seq {
            yield! titleUpgrades hits

            if String.isNotEmpty imageUri then
                yield imgLink imageUri

            yield! formatHits hits
            yield! footer
        }

    let generateList hits =
        seq {
            yield! titleList ()
            yield! formatHits hits
            yield! footer
        }
