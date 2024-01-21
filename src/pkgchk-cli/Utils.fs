namespace pkgchk

open System
open System.Diagnostics
open System.Diagnostics.CodeAnalysis

[<ExcludeFromCodeCoverage>]
module Combinators =
    let (&&>>) x y = (fun (v: 'a) -> x v && y v)

    let (||>>) x y = (fun (v: 'a) -> x v || y v)

module String =
    [<DebuggerStepThrough>]
    let join separator (lines: seq<string>) = String.Join(separator, lines)

    [<DebuggerStepThrough>]
    let joinLines (lines: seq<string>) = join Environment.NewLine lines

    [<DebuggerStepThrough>]
    let joinPretty separator finalSeparator (values: string list) =
        let separator = $" {separator} "
        let finalSeparator = $" {finalSeparator} "

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
    let indent length = new String(' ', length)

    [<DebuggerStepThrough>]
    let nullToEmpty (value: string) =
        if obj.ReferenceEquals(value, null) then "" else value

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
        match
            Assembly
                .GetExecutingAssembly()
                .GetCustomAttributes<AssemblyInformationalVersionAttribute>()
            |> Seq.take 1
            |> List.ofSeq
        with
        | [ x ] -> x.InformationalVersion
        | _ -> ""
