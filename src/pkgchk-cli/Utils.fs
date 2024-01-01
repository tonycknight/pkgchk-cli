namespace pkgchk

open System
open System.Diagnostics

module String =
    [<DebuggerStepThrough>]
    let join (separator) (lines: seq<string>) = String.Join(separator, lines)

    [<DebuggerStepThrough>]
    let joinLines (lines: seq<string>) = join Environment.NewLine lines

    [<DebuggerStepThrough>]
    let isEmpty = String.IsNullOrEmpty

    // TODO: negation/whitespace