namespace pkgchk.reporting

open pkgchk

type MarkdownTable =
    { columns: string list
      rows: string list list }

    static member Default =
        { MarkdownTable.columns = []
          rows = [] }

    static member addColumn name table =
        { table with
            columns = name :: table.columns }

    static member addRow row table = { table with rows = row :: table.rows }

module Markdown =

    let h1 (value: string) = $"# {value}"
    let h2 (value: string) = $"# {value}"
    let h3 (value: string) = $"# {value}"
    let dividor () = "---"

    let table (value: MarkdownTable) =
        let surround line = $"| {line} |"
        let columnCount = Seq.length value.columns

        let fmtRow (cols: string list) =
            cols |> Seq.truncate columnCount |> String.join " | "

        let lines =
            seq {
                yield value.columns |> Seq.rev |> String.join " | "
                yield [ 1..columnCount ] |> Seq.map (fun _ -> "-") |> String.join " | "
                yield! value.rows |> Seq.rev |> Seq.map (List.ofSeq >> fmtRow)
            }

        lines |> Seq.map surround |> String.join System.Environment.NewLine

    let italic (value: string) = $"_{value}_"

    let escape (value: string) =
        value.Replace('\r', ' ').Replace('\n', ' ')

    let colourise colour value =
        $"<span style='color:{colour}'>{value}</span>"

    let hyperlink name url = $"[{name}]({url})"

    let link url = hyperlink url url

    let image uri = $"![image]({uri})"
