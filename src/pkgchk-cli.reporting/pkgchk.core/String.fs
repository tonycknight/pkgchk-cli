namespace pkgchk

open System
open System.Diagnostics

module String =
    [<DebuggerStepThrough>]
    let join separator (lines: seq<string>) = String.Join(separator, lines)

    [<DebuggerStepThrough>]
    let joinLines (lines: seq<string>) = join Environment.NewLine lines

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
