namespace pkgchk

open System
open System.Diagnostics

module String =

    [<DebuggerStepThrough>]
    let join separator (lines: seq<string>) = String.Join(separator, lines)

    [<DebuggerStepThrough>]
    let joinLines (lines: seq<string>) = join Environment.NewLine lines

    [<DebuggerStepThrough>]
    let joinPretty separator finalSeparator (values: string seq) =

        let rec concat (values: string list) (accum: System.Text.StringBuilder) =
            let suffix (sep: string) (accum: System.Text.StringBuilder) =
                if accum.Length > 0 then accum.Append(sep) else accum

            match values with
            | [] -> accum.ToString()
            | [ x ] ->
                let accum = accum |> suffix finalSeparator
                accum.Append(x).ToString()
            | h :: t ->
                let accum = accum |> suffix separator
                accum.Append(h) |> concat t

        concat (List.ofSeq values) (new System.Text.StringBuilder())

    [<DebuggerStepThrough>]
    let isEmpty = String.IsNullOrWhiteSpace

    [<DebuggerStepThrough>]
    let isNotEmpty = isEmpty >> not

    [<DebuggerStepThrough>]
    let trim (value: string) = value.Trim()

    [<DebuggerStepThrough>]
    let defaultValue (defaultValue: string) (value: string) =
        if isNotEmpty value then value else defaultValue

    [<DebuggerStepThrough>]
    let leading (len: int) (value: string) =
        let len2 = System.Math.Min(value.Length, len)

        if value.Length <= len2 then
            value
        else
            (value.Substring(0, len2) + "...")

    [<DebuggerStepThrough>]
    let nonEmpty (value: string) =
        if String.IsNullOrEmpty value then None else Some value

    [<DebuggerStepThrough>]
    let append (separator: string) (x: string) (y: string) =
        if y.Length = 0 then x
        else if x.Length = 0 then y
        else $"{y}{separator}{x}"

    [<DebuggerStepThrough>]
    let escapeMarkup (value: string) = // TODO: to console rendering module...?
        value.Replace("[", "[[").Replace("]", "]]")

    [<DebuggerStepThrough>]
    let isInt (value: string) = Int32.TryParse value |> fst

    [<DebuggerStepThrough>]
    let toInt (value: string) =
        match Int32.TryParse value with
        | (true, x) -> x
        | _ -> 0

    [<DebuggerStepThrough>]
    let toLower (value: string) = value.ToLowerInvariant()

    [<DebuggerStepThrough>]
    let split (delim: char) (value: string) =
        match value.Split(delim, StringSplitOptions.None) with
        | [| x; y |] -> (x, y)
        | _ -> ("", value)

    [<DebuggerStepThrough>]
    let trimlines (value: string) =
        value.Split([| '\n'; '\r' |], System.StringSplitOptions.RemoveEmptyEntries)
        |> Seq.map trim
        |> Seq.filter isNotEmpty
        |> join " "
