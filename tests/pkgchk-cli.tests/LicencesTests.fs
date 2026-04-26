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

    [<Theory>]
    [<InlineData("MIT", "MIT")>]
    [<InlineData("MIT OR Apache-2.0", "MIT|Apache-2.0")>]
    [<InlineData("MIT AND Apache-2.0", "MIT|Apache-2.0")>]
    let ``licence extracts licences`` (expression, expected: string) =
        let metadata =
            { pkgchk.NugetPackageMetadata.empty with
                license = expression }

        let hit =
            { pkgchk.ScaHit.empty with
                metadata = Some metadata }

        let result = pkgchk.Licences.licence hit
        let expected = expected.Split('|')
        result |> should equalSeq expected

    [<Theory>]
    [<InlineData("MIT", "MIT")>]
    [<InlineData("MIT", "mit")>]
    [<InlineData("mit", "MIT")>]
    [<InlineData("MIT|Apache-2.0", "MIT")>]
    [<InlineData("MIT|Apache-2.0", "Apache-2.0")>]
    [<InlineData("MIT|Apache-2.0", "MIT|Apache-2.0")>]
    [<InlineData("mit|apache-2.0", "MIT|Apache-2.0")>]
    let ``isMemberOf returns true on match`` (licences: string, referenceLicences: string) =
        let licences = licences.Split('|')
        let referenceLicences = referenceLicences.Split('|')
        
        let result = referenceLicences |> pkgchk.Licences.isMemberOf licences

        result |> should be True

    [<Theory>]
    [<InlineData("MIT", "Apache-2.0")>]
    [<InlineData("aaa|bbb", "MIT|Apache-2.0")>]
    let ``isMemberOf returns false on mismatch`` (licences: string, referenceLicences: string) =
        let licences = licences.Split('|')
        let referenceLicences = referenceLicences.Split('|')
        
        let result = referenceLicences |> pkgchk.Licences.isMemberOf licences

        result |> should be False
