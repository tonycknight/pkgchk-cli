namespace pkgchk

open System
open Spectre.Console

module Console =

    let italic value = $"[italic]{value}[/]"

    let formatReason value =
        let colour = Rendering.reasonColour value
        $"[{colour}]{value}[/]"

    let formatReasons = Seq.map formatReason >> String.join ", "

    let formatSeverity value =
        let code =
            $"{Rendering.severityStyle value} {Rendering.severityColour value}"
            |> String.trim

        $"[{code}]{value}[/]"

    let nugetLinkPkgVsn package version =
        let url = $"{Rendering.nugetPrefix}/{package}/{version}"
        $"[link={url}]{package} {version}[/]"

    let nugetLinkPkgSuggestion package suggestion =
        let url = $"{Rendering.nugetPrefix}/{package}"
        $"[link={url}]{package} {suggestion}[/]"

    let formatProject value = $"[bold yellow]{value}[/]"

    let formatHits (hits: seq<ScaHit>) =

        let fmt (hit: ScaHit) =
            seq {
                match hit.kind with
                | ScaHitKind.Vulnerability ->
                    sprintf
                        "%s: %s - [cyan]%s[/]"
                        (Rendering.formatHitKind hit.kind)
                        (formatSeverity hit.severity)
                        (nugetLinkPkgVsn hit.packageId hit.resolvedVersion)
                        
                | ScaHitKind.Deprecated ->
                    sprintf
                        "%s: [cyan]%s[/]"
                        (Rendering.formatHitKind hit.kind)
                        (nugetLinkPkgVsn hit.packageId hit.resolvedVersion)
                        
                if String.isNotEmpty hit.advisoryUri then
                    sprintf "                    %s" (italic hit.advisoryUri)

                if
                    (hit.reasons |> Array.isEmpty |> not)
                    && String.isNotEmpty hit.suggestedReplacement
                then
                    sprintf
                        "                    [italic]%s - use [cyan]%s[/][/]"
                        (formatReasons hit.reasons)
                        (match (hit.suggestedReplacement, hit.alternativePackageId) with
                         | "", _ -> ""
                         | x, y when x <> "" && y <> "" -> nugetLinkPkgSuggestion y x |> sprintf "Use %s"
                         | x, _ -> x |> sprintf "Use %s")
                else if (hit.reasons |> Array.isEmpty |> not) then
                    sprintf "                    %s" (italic (formatReasons hit.reasons))

                ""
            }

        let fmtGrp (hit: (string * seq<ScaHit>)) =
            let projectPath, hits = hit

            let hits =
                hits
                |> Seq.sortBy (fun h ->
                    (match h.kind with
                     | ScaHitKind.Vulnerability -> 0
                     | _ -> 1),
                    h.packageId)

            seq {
                $"Project: {projectPath}" |> formatProject
                yield! hits |> Seq.collect fmt
            }

        hits
        |> Seq.groupBy (fun h -> h.projectPath)
        |> Seq.sortBy fst
        |> Seq.collect fmtGrp

    let title hits =
        match hits with
        | [] -> seq { "[lime]No vulnerabilities found.[/]" }
        | _ -> seq { "[red]Vulnerabilities found![/]" }

    let formatSeverities severities =
        severities
        |> Seq.map formatSeverity
        |> List.ofSeq
        |> String.joinPretty "," "or"
        |> sprintf "Vulnerabilities found matching %s"
        |> italic
        |> Seq.singleton


    let formatHitCounts counts =
        counts
        |> Seq.map (fun (k, s, c) ->
            let fmtCount value =
                match value with
                | 1 -> $"{value} hit"
                | _ -> $"{value} hits"

            let fmtSeverity =
                function
                | ScaHitKind.Vulnerability -> formatSeverity
                | ScaHitKind.Deprecated -> formatReason

            $"{Rendering.formatHitKind k} - {fmtSeverity k s}: {fmtCount c}.")
        |> List.ofSeq

    let error value = $"[red]{value}[/]"

    let reportFileBuilt path =
        $"Report file [link={path}]{path}[/] built." |> italic

    let send (console: IAnsiConsole) = console.MarkupLine
