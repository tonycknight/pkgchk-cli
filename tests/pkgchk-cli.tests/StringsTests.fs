namespace pkgchk.tests

open System
open FsCheck
open FsCheck.Xunit


module StringsTests =

    [<Property>]
    let ``join conjugates strings`` (count: PositiveInt) =
        let values = [ 0 .. count.Get ] |> Seq.map (fun _ -> "A") |> Array.ofSeq

        let result = values |> pkgchk.String.join " "

        let expected = result.Split(' ', StringSplitOptions.None)

        expected = values

    [<Property(Verbose = true)>]
    let ``joinPretty has correct value count`` (count: PositiveInt) =
        let values = [ 1 .. count.Get ] |> Seq.map (fun _ -> "A") |> List.ofSeq
        let finalSeparator = "or"
        let separator = ","

        let result = values |> pkgchk.String.joinPretty separator finalSeparator

        let counts =
            result.Split(' ', StringSplitOptions.None)
            |> Array.map (fun s -> s.Replace(separator, ""))
            |> Array.countBy id
            |> Map.ofSeq

        counts.["A"] = count.Get


    [<Property(Verbose = true)>]
    let ``joinPretty has correct intermediary separators`` (count: PositiveInt) =
        let values = [ 1 .. count.Get ] |> Seq.map (fun _ -> "A") |> List.ofSeq
        let finalSeparator = "or"
        let separator = ','

        let result = values |> pkgchk.String.joinPretty separator finalSeparator

        let decomp = result.ToCharArray() |> Array.filter (fun c -> c = separator)

        match (count.Get, decomp) with
        | (x, [||]) when x <= 2 -> true
        | _ ->
            let counts = decomp |> Array.countBy id |> Map.ofSeq
            counts.[separator] = count.Get - 2


    [<Property(Verbose = true)>]
    let ``joinPretty has correct last separator`` (count: PositiveInt) =
        let values = [ 1 .. count.Get ] |> Seq.map (fun _ -> "A") |> List.ofSeq
        let finalSeparator = "or"
        let separator = ","

        let result = values |> pkgchk.String.joinPretty separator finalSeparator

        let decomp = result.Split(' ', StringSplitOptions.None) |> Array.rev

        let counts = decomp |> Array.countBy id |> Map.ofSeq

        match count.Get with
        | 1 -> counts.["A"] = count.Get && decomp |> Array.contains finalSeparator |> not
        | x ->
            let pos = Array.IndexOf(decomp, finalSeparator)

            counts.[finalSeparator] = 1 && counts.["A"] = count.Get && pos = 1
