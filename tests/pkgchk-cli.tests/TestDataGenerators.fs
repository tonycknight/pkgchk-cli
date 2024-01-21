namespace pkgchk.tests

open System
open FsCheck
open pkgchk.Combinators

type AlphaNumericString =

    static member Generate() =
        let isAlphaNumeric (value: string) =
            value |> Seq.forall (Char.IsLetter ||>> Char.IsNumber)

        let isNotNullOrEmpty = String.IsNullOrEmpty >> not

        Arb.Default.String() |> Arb.filter (isNotNullOrEmpty &&>> isAlphaNumeric)
