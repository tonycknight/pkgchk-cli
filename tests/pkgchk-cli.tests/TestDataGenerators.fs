namespace pkgchk.tests

open System
open FsCheck
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

type VulnerableScaHitKind =

    static member Generate() =
        let kinds =
            [ pkgchk.ScaHitKind.Vulnerability; pkgchk.ScaHitKind.VulnerabilityTransitive ]

        Gen.elements kinds |> Arb.fromGen


type DeprecatedScaHitKind =

    static member Generate() =
        let kinds = [ pkgchk.ScaHitKind.Deprecated ]
        Gen.elements kinds |> Arb.fromGen

type KnownHitSeverity =

    static member Generate() =
        let kinds = [ "High"; "Critical"; "Moderate" ]
        Gen.elements kinds |> Arb.fromGen

type KnownHitReason =

    static member Generate() =
        let kinds = [ "Critical Bugs"; "Legacy"; "Other" ]
        Gen.elements kinds |> Arb.fromGen
