namespace pkgchk

open System.Diagnostics.CodeAnalysis

[<ExcludeFromCodeCoverage>]
module Combinators =
    let (&&>>) x y = (fun (v: 'a) -> x v && y v)

    let (||>>) x y = (fun (v: 'a) -> x v || y v)
