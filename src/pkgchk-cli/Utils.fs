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
    let indent (pad: int) (value: string) = $"{new String(' ', pad)}{value}"

    [<DebuggerStepThrough>]
    let escapeMarkup (value: string) =
        value.Replace("[", "[[").Replace("]", "]]")

    [<DebuggerStepThrough>]
    let isInt (value: string) = Int32.TryParse value |> fst

    [<DebuggerStepThrough>]
    let toInt (value: string) =
        match Int32.TryParse value with
        | (true, x) -> x
        | _ -> 0

    [<DebuggerStepThrough>]
    let split (delim: char) (value: string) =
        match value.Split(delim, StringSplitOptions.None) with
        | [| x; y |] -> (x, y)
        | _ -> ("", value)

    [<DebuggerStepThrough>]
    let toLower (value: string) = value.ToLowerInvariant()

    [<DebuggerStepThrough>]
    let append (suffix: string) (value: string) = $"{value}{suffix}"

module Option =
    let nullDefault<'a> (defaultValue: 'a) (value: 'a) =
        if obj.ReferenceEquals(value, null) then
            defaultValue
        else
            value

    let isNull<'a> (value: 'a) = obj.ReferenceEquals(value, null)

    let ofNull<'a> (value: 'a) =
        if obj.ReferenceEquals(value, null) then
            None
        else
            Some value

    let nonEmpty (value: string) =
        if String.IsNullOrEmpty value then None else Some value

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

module Environment =

    [<ExcludeFromCodeCoverage>]
    let isRunningGithub =
        System.Environment.GetEnvironmentVariable("GITHUB_ACTIONS") <> null

module Json =
    open Newtonsoft.Json

    let serialise<'a> =
        let settings = new JsonSerializerSettings()
        settings.Formatting <- Formatting.Indented
        settings.Converters.Add(new Converters.StringEnumConverter())
        
        fun (value: 'a) -> JsonConvert.SerializeObject(value, settings)

module PrettyJson =
    open System.Text.Json.Serialization
    open System.Text.Json

    let options = JsonFSharpOptions.Default().ToJsonSerializerOptions()

    let enumLikeOptions () =
        JsonFSharpOptions.Default().WithUnionUnwrapFieldlessTags().WithMapFormat(MapFormat.Object)

    let serialise<'a> =
        let opts = enumLikeOptions().ToJsonSerializerOptions()
        opts.Converters.Add(new JsonStringEnumConverter())
        opts.WriteIndented <- true
        
        fun (value: 'a) -> JsonSerializer.Serialize (value, opts)
