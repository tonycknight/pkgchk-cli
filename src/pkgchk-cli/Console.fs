﻿namespace pkgchk

open System
open Spectre.Console

module Console =

    [<Literal>]
    let validationOk = 0

    [<Literal>]
    let validationFailed = 1

    [<Literal>]
    let sysError = 2

    let formatHitKind =
        function
        | ScaHitKind.Vulnerability -> "Vulnerable package"
        | ScaHitKind.Deprecated -> "Deprecated package"

    let formatReason value =
        match value with
        | "Legacy" -> sprintf "[yellow]%s[/]" value
        | _ -> sprintf "[red]%s[/]" value

    let formatSeverity value =
        let code =
            match value with
            | "High" -> "red"
            | "Critical" -> "italic red"
            | "Moderate" -> "#d75f00"
            | _ -> "yellow"

        sprintf "[%s]%s[/]" code value

    let formatProject value = sprintf "[bold yellow]%s[/]" value

    let formatHits (hits: seq<ScaHit>) =

        let fmt (hit: ScaHit) =
            seq {
                match hit.kind with
                | ScaHitKind.Vulnerability ->
                    sprintf
                        "%s: %s - [cyan]%s[/] version [cyan]%s[/]"
                        (formatHitKind hit.kind)
                        (formatSeverity hit.severity)
                        hit.packageId
                        hit.resolvedVersion
                | ScaHitKind.Deprecated ->
                    sprintf
                        "%s: [cyan]%s[/] version [cyan]%s[/]"
                        (formatHitKind hit.kind)
                        hit.packageId
                        hit.resolvedVersion

                if String.isNotEmpty hit.advisoryUri then
                    sprintf "                    [italic]%s[/]" hit.advisoryUri

                if String.isNotEmpty hit.reasons && String.isNotEmpty hit.suggestedReplacement then
                    sprintf
                        "                    [italic]%s - use [cyan]%s[/][/]"
                        (formatReason hit.reasons)
                        hit.suggestedReplacement
                else if String.isNotEmpty hit.reasons then
                    sprintf "                    [italic]%s " hit.reasons

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

    let noVulnerabilities () =
        "[bold green]No vulnerabilities found.[/]"

    let vulnerabilities hits =
        seq {
            "[bold red]Vulnerabilities found![/]"
            yield! formatHits hits
        }
        |> String.joinLines

    let error (error: string) = sprintf "[red]%s[/]" error

    let reportFileBuilt path =
        sprintf "[italic]Report file %s built.[/]" path

    let send (console: IAnsiConsole) = console.MarkupLine
