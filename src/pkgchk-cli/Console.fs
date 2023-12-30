namespace pkgchk

open System
open System.Diagnostics.CodeAnalysis
open Spectre.Console

[<ExcludeFromCodeCoverage>]
module Console =
    let returnNoVulnerabilities (console: IAnsiConsole) =
        "[bold green]No vulnerabilities found.[/]"
        |> console.Markup
        |> console.WriteLine

        0

    let returnVulnerabilities (console: IAnsiConsole) hits =
        "[bold red]Vulnerabilities found![/]" |> console.Markup |> console.WriteLine

        hits |> Sca.formatHits |> console.WriteLine
        1

    let returnError (console: IAnsiConsole) (error: string) =
        console.WriteLine error
        2
