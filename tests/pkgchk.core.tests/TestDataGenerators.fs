namespace pkgchk.core.tests

open System
open FsCheck.FSharp
open pkgchk.Combinators

[<AutoOpen>]
module Arbitraries =
    let isAlphaNumeric (value: string) =
        value |> Seq.forall (Char.IsLetter ||>> Char.IsNumber)

    let isNotNullOrEmpty = String.IsNullOrEmpty >> not

    let isValidString = isNotNullOrEmpty &&>> isAlphaNumeric

type AlphaNumericString =

    static member Generate() =
        ArbMap.defaults |> ArbMap.arbitrary<string> |> Arb.filter isValidString

type AlphaNumericStringSingletonArray =

    static member Generate() =
        ArbMap.defaults
        |> ArbMap.generate<string>
        |> Gen.filter isValidString
        |> Gen.map (fun s -> [| s |])
        |> Arb.fromGen

