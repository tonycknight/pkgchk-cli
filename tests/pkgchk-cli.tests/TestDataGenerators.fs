namespace pkgchk.tests

open System
open FsCheck
open pkgchk.Combinators

[<AutoOpen>]
module Arbitraries =
    let isAlphaNumeric (value: string) =
        value |> Seq.forall (Char.IsLetter ||>> Char.IsNumber)

    let isNotNullOrEmpty = String.IsNullOrEmpty >> not

type AlphaNumericString =

    static member Generate() =
        Arb.Default.String() |> Arb.filter (isNotNullOrEmpty &&>> isAlphaNumeric)

type AlphaNumericStringArray =

    static member Generate() =
        Arb.generate<string>
        |> Gen.filter (isNotNullOrEmpty &&>> isAlphaNumeric)
        |> Gen.map (fun s -> [| s |])
        |> Arb.fromGen

type VulnerableScaHitKind =
    
    static member Generate() =
        let kinds = [ pkgchk.ScaHitKind.Vulnerability; pkgchk.ScaHitKind.VulnerabilityTransitive ]
        Gen.elements kinds
        |> Arb.fromGen
        
        
type DeprecatedScaHitKind =
    
    static member Generate() =
        let kinds = [ pkgchk.ScaHitKind.Deprecated ]
        Gen.elements kinds
        |> Arb.fromGen
