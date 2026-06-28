namespace pkgchk.reporting.tests

open System
open FsCheck
open FsCheck.Xunit
open pkgchk.reporting

module ModelsTests =

    [<Xunit.Fact>]
    let ``Table.empty produces empty table`` () =
        let t = Table.empty

        t.rows = [] && t.columns = [] && t.border = false && t.showHeaders = false


    [<Property(Arbitrary = [| typeof<AlphaNumericString> |], Verbose = true)>]
    let ``Table.singleRow generates single row single cell table`` (value: string) =
        let t = Table.singleRow value

        t.columns = [ "" ]  && t.rows = [ [value] ] && t.border = false && t.showHeaders = false