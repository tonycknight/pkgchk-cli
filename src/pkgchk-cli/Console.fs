namespace pkgchk

open System.Diagnostics.CodeAnalysis
open Spectre.Console

[<ExcludeFromCodeCoverage>]
module Console =
    let noVulnerabilities (console: IAnsiConsole) =
        "[bold green]No vulnerabilities found.[/]"
        |> console.Markup
        |> console.WriteLine

    let vulnerabilities (console: IAnsiConsole) hits =
        "[bold red]Vulnerabilities found![/]" |> console.Markup |> console.WriteLine
        hits |> Sca.formatHits |> console.WriteLine

    let error (console: IAnsiConsole) (error: string) = console.WriteLine error

    [<Literal>]
    let validationOk = 0

    [<Literal>]
    let validationFailed = 1

    [<Literal>]
    let sysError = 2
