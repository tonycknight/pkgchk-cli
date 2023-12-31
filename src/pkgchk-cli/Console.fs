namespace pkgchk

open System
open Spectre.Console

module Console =

    [<Literal>]
    let validationOk = 0

    [<Literal>]
    let validationFailed = 1

    [<Literal>]
    let sysError = 2

    let joinLines (lines: seq<string>) = String.Join(Environment.NewLine, lines)

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
                ""
                sprintf "Project:          %s" hit.projectPath |> formatProject
                sprintf "Severity:         %s" (formatSeverity hit.severity)
                sprintf "Package:          [cyan]%s[/] version [cyan]%s[/]" hit.packageId hit.resolvedVersion
                sprintf "Advisory URL:     %s" hit.advisoryUri
            }

        hits |> Seq.collect fmt


    let noVulnerabilities () =
        "[bold green]No vulnerabilities found.[/]"

    let vulnerabilities hits =
        seq {
            "[bold red]Vulnerabilities found![/]"
            yield! formatHits hits
        }
        |> joinLines

    let error (error: string) = sprintf "[red]%s[/]" error

    let reportFileBuilt path =
        sprintf "[italic]Report file %s built.[/]" path

    let send (console: IAnsiConsole) = console.MarkupLine
