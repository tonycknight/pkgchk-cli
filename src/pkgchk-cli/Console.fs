namespace pkgchk

open System
open System.Diagnostics.CodeAnalysis
open Spectre.Console

[<ExcludeFromCodeCoverage>]
module Console =
    let returnNoVulnerabilities () =
        "[bold green]No vulnerabilities found![/]"
        |> AnsiConsole.Markup
        |> Console.Out.WriteLine

        0

    let returnVulnerabilities hits =
        "[bold red]Vulnerabilities found![/]"
        |> AnsiConsole.Markup
        |> Console.Out.WriteLine

        hits |> Sca.formatHits |> Console.Out.WriteLine
        1

    let returnError (error: string) =
        Console.Error.WriteLine error
        2
