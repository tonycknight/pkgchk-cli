namespace pkgchk.reporting

open pkgchk
open Spectre.Console
open Spectre.Console.Rendering

module Console =
    let toRenderable x =
        x :> Spectre.Console.Rendering.IRenderable

    let markup (style: string) (value: string) = $"[{style}]{value}[/]"
    let italic = markup "italic"
    let white = markup Colours.white
    let green = markup Colours.green
    let cyan = markup Colours.cyan
    let lightcyan = markup Colours.lightcyan
    let darkcyan = markup Colours.darkcyan
    let yellow = markup Colours.yellow
    let orange = markup Colours.orange
    let blue = markup Colours.cornflowerblue
    let error = markup Colours.red
    let lightgrey = markup Colours.lightgrey

    let grey =
        match Environment.isRunningGithub with
        | true -> Colours.lightgrey
        | _ -> Colours.grey
        |> markup

    let table (value: pkgchk.reporting.Table) =
        let table = new Table()

        table.Border <-
            match value.border with
            | false -> TableBorder.None
            | true -> TableBorder.Rounded

        table.ShowHeaders <- value.showHeaders

        value.columns |> Seq.iter (fun c -> table.AddColumn(c) |> ignore)
        value.rows |> Seq.iter (fun r -> r |> Array.ofSeq |> table.AddRow |> ignore)

        table
