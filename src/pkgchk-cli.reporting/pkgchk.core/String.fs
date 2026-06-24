namespace pkgchk

open System
open System.Diagnostics

module String =
    [<DebuggerStepThrough>]
    let join separator (lines: seq<string>) = String.Join(separator, lines)

    [<DebuggerStepThrough>]
    let joinLines (lines: seq<string>) = join Environment.NewLine lines

    [<DebuggerStepThrough>]
    let joinPretty separator finalSeparator (values: string list) =

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

        concat values (new System.Text.StringBuilder())

    [<DebuggerStepThrough>]
    let isEmpty = String.IsNullOrWhiteSpace

    [<DebuggerStepThrough>]
    let isNotEmpty = isEmpty >> not

    [<DebuggerStepThrough>]
    let trim (value: string) = value.Trim()

    [<DebuggerStepThrough>]
    let nonEmpty (value: string) =
        if String.IsNullOrEmpty value then None else Some value

    [<DebuggerStepThrough>]
    let append (separator: string) (x: string) (y: string) =
        if y.Length = 0 then x
        else if x.Length = 0 then y
        else $"{y}{separator}{x}"
