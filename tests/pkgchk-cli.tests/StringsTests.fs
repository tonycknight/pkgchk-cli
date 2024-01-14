namespace pkgchk.tests

open System
open FsUnit.Xunit
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
    let ``joinPretty`` (count: PositiveInt) =
        let values = [ 1 .. count.Get ] |> Seq.map (fun _ -> "A") |> List.ofSeq
        let separator = "or"

        let result = values |> pkgchk.String.joinPretty separator

        let decomp =
            result.Split(' ', StringSplitOptions.None)
            |> Array.map (fun s -> s.Replace(",", ""))
            |> Array.rev

        let counts = decomp |> Array.countBy id |> Map.ofSeq

        match count.Get with
        | 1 -> counts.["A"] = count.Get && decomp |> Array.contains separator |> not

        | x ->
            let pos = Array.IndexOf(decomp, separator)

            counts.[separator] = 1 && counts.["A"] = count.Get && pos = 1
