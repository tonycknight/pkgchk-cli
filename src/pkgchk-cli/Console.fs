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

        let lines = hits |> Seq.collect fmt
        String.Join(Environment.NewLine, lines) |> AnsiConsole.MarkupLine

    let noVulnerabilities (console: IAnsiConsole) =
        "[bold green]No vulnerabilities found.[/]"
        |> console.Markup
        |> console.WriteLine

    let vulnerabilities (console: IAnsiConsole) hits =
        "[bold red]Vulnerabilities found![/]" |> console.Markup |> console.WriteLine
        hits |> formatHits |> console.WriteLine

    let error (console: IAnsiConsole) (error: string) = console.WriteLine error

    let reportFileBuilt (console: IAnsiConsole) path =
        path
        |> sprintf "[italic]Report file %s built.[/]"
        |> console.Markup
        |> console.WriteLine
