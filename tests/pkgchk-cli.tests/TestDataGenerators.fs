namespace pkgchk.tests

open System
open FsCheck

type AlphaNumericString =

    static member Generate() =
        let isAlphaNumeric (value: string) =
            value |> Seq.forall (fun c -> Char.IsLetter c || Char.IsNumber c)

        let isNotNullOrEmpty = String.IsNullOrEmpty >> not

        Arb.Default.String()
        |> Arb.filter (fun a -> isNotNullOrEmpty a && isAlphaNumeric a)
