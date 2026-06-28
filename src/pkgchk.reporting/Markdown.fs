namespace pkgchk.reporting

open pkgchk

module Markdown =
    let hdr (depth: int) (value: string) = 
        let prefix = new string('#', depth)
        $"{prefix} {value}"

    let dividor () = "---"

    let table (value: Table) =
        let surround line = $"| {line} |"
        let columnCount = Seq.length value.columns

        let fmtRow (cols: string list) =
            cols |> Seq.truncate columnCount |> String.join " | "

        let lines =
            seq {
                yield value.columns |> String.join " | "
                yield [ 1..columnCount ] |> Seq.map (fun _ -> "-") |> String.join " | "
                yield! value.rows |> Seq.map (List.ofSeq >> fmtRow)
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
