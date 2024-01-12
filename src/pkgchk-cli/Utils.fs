namespace pkgchk

open System
open System.Diagnostics

module String =
    [<DebuggerStepThrough>]
    let join (separator) (lines: seq<string>) = String.Join(separator, lines)

    [<DebuggerStepThrough>]
    let joinLines (lines: seq<string>) = join Environment.NewLine lines

    [<DebuggerStepThrough>]
    let isEmpty = String.IsNullOrWhiteSpace

    [<DebuggerStepThrough>]
    let isNotEmpty = isEmpty >> not

    [<DebuggerStepThrough>]
    let trim (value: string) = value.Trim()

module ReturnCodes =

    [<Literal>]
    let validationOk = 0

    [<Literal>]
    let validationFailed = 1

    [<Literal>]
    let sysError = 2

module HashSet =
    open System.Collections.Generic

    let ofSeq<'a> (comparer: IEqualityComparer<'a>) (values: seq<'a>) = new HashSet<'a>(values, comparer)

    let contains<'a> (hashSet: HashSet<'a>) = hashSet.Contains

open System.Reflection

module App =
    let version () =
        match Assembly.GetExecutingAssembly().GetCustomAttributes<AssemblyInformationalVersionAttribute>() |> Seq.take 1 |> List.ofSeq with
        | [x] -> x.InformationalVersion
        | _ -> ""