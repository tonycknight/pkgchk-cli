namespace pkgchk.tests

open FsUnit.Xunit
open Xunit

module LicencesTests =
    [<Fact>]
    let ``parse simple licence`` () =
        let result = pkgchk.Licences.parse "MIT"

        result |> Seq.length |> should equal 1
        result |> Seq.head |> should equal "MIT"

    [<Theory>]
    [<InlineData("MIT AND Apache-2.0")>]
    [<InlineData("MIT OR Apache-2.0")>]
    [<InlineData("MIT AND Apache-2.0+ WITH gnu-javamail-exception")>]
    [<InlineData("(MIT AND Apache-2.0+ AND DocumentRef-foo:LicenseRef-bar) WITH gnu-javamail-exception")>]
    let ``parse compound expression`` (expression) =
        let result = pkgchk.Licences.parse expression

        let expected = [ "MIT"; "Apache-2.0" ]

        result |> should equalSeq expected

    [<Theory>]
    [<InlineData("")>]
    [<InlineData(".")>]
    [<InlineData("!")>]
    [<InlineData("+")>]
    [<InlineData("blablabla")>]
    let ``parse unknown licence`` (expression) =
        let result = pkgchk.Licences.parse expression

        result |> Seq.length |> should equal 1
        result |> Seq.head |> should equal expression
