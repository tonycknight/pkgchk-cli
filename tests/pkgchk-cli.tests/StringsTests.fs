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
        let word = "A"
        let finalSeparator = " or "
        let separator = ", "
        let values = [ 1 .. count.Get ] |> Seq.map (fun _ -> word) |> List.ofSeq
        
        let result = values |> pkgchk.String.joinPretty separator finalSeparator

        let decomp = result.Split([| word |], StringSplitOptions.RemoveEmptyEntries)

        decomp.Length = count.Get - 1



    [<Property(Verbose = true)>]
    let ``joinPretty has correct intermediary separators`` (count: PositiveInt) =
        let word = "A"
        let finalSeparator = " or "
        let separator = ", "
        let values = [ 1 .. count.Get ] |> Seq.map (fun _ -> word) |> List.ofSeq

        let result = values |> pkgchk.String.joinPretty separator finalSeparator

        let decomp = result.Split([| word |], StringSplitOptions.RemoveEmptyEntries) 
        
        let matches = decomp |> Array.filter (fun d -> d = separator)
        matches.Length = Math.Max(0, count.Get - 2)
        

    [<Property(Verbose = true)>]
    let ``joinPretty has correct last separator`` (count: PositiveInt) =
        let word = "A"
        let finalSeparator = " or "
        let separator = ", "
        let values = [ 1 .. count.Get ] |> Seq.map (fun _ -> word) |> List.ofSeq

        let result = values |> pkgchk.String.joinPretty separator finalSeparator
                
        match result.Split([| word |], StringSplitOptions.RemoveEmptyEntries)  with
        | [||] -> true
        | xs -> 
            let idx = xs |> Array.findIndex (fun d -> d = finalSeparator)
            idx = xs.Length - 1
