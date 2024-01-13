namespace pkgchk

open System
open Spectre.Console

module Console =

    let formatReasons values =
        let formatReason value =
            let colour = Rendering.reasonColour value
            $"[{colour}]{value}[/]"

        values |> Seq.map formatReason |> String.join ", "

    let formatSeverity value =
        let code =
            $"{Rendering.severityStyle value} {Rendering.severityColour value}"
            |> String.trim

        sprintf "[%s]%s[/]" code value

    let nugetLinkPkgVsn package version =
        let url = $"https://www.nuget.org/packages/{package}/{version}"
        $"[link={url}]{package} {version}[/]"

    let nugetLinkPkgSuggestion package suggestion =
        let url = $"https://www.nuget.org/packages/{package}"
        $"[link={url}]{package} {suggestion}[/]"

    let formatProject value = sprintf "[bold yellow]%s[/]" value

    let formatHits (hits: seq<ScaHit>) =

        let fmt (hit: ScaHit) =
            seq {
                match hit.kind with
                | ScaHitKind.Vulnerability ->
                    sprintf
                        "%s: %s - [cyan]%s[/] version [cyan]%s[/]"
                        (Rendering.formatHitKind hit.kind)
                        (formatSeverity hit.severity)
                        (nugetLinkPkgVsn hit.packageId hit.resolvedVersion)
                        hit.resolvedVersion
                | ScaHitKind.Deprecated ->
                    sprintf
                        "%s: [cyan]%s[/] version [cyan]%s[/]"
                        (Rendering.formatHitKind hit.kind)
                        (nugetLinkPkgVsn hit.packageId hit.resolvedVersion)
                        hit.resolvedVersion

                if String.isNotEmpty hit.advisoryUri then
                    sprintf "                    [italic]%s[/]" hit.advisoryUri

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
                    sprintf "                    [italic]%s[/]" (formatReasons hit.reasons)

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
                sprintf "Project: %s" projectPath |> formatProject
                yield! hits |> Seq.collect fmt
            }

        let grps = hits |> Seq.groupBy (fun h -> h.projectPath) |> Seq.sortBy fst
        (grps |> Seq.collect fmtGrp)

    let title hits =
        match hits with
        | [] -> seq { "[green]No vulnerabilities found.[/]" }
        | _ -> seq { "[red]Vulnerabilities found![/]" }

    let error (error: string) = sprintf "[red]%s[/]" error

    let reportFileBuilt path =
        sprintf "[italic]Report file [link=%s]%s[/] built.[/]" path path

    let send (console: IAnsiConsole) = console.MarkupLine
