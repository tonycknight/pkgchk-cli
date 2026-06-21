namespace pkgchk.tests.integration

open Xunit

type PackageLookupTests(output: ITestOutputHelper) =

    [<Fact>]
    let ``Lookup without package ID returns error`` () =

        runPkgChkLookupArgs "" "" false false false |> execSysErrorPkgChk output

    [<Theory>]
    [<InlineData("xunit", "2.9.3")>]
    let ``Lookup with package ID returns correct info`` packageId version =

        runPkgChkLookupArgs packageId "" false false false
        |> execSuccessPkgChk output
        |> assertPackagesFound [ version ]

    [<Theory>]
    [<InlineData("xunit", "2.9.3")>]
    let ``Lookup with package ID and version returns correct info`` packageId version =

        runPkgChkLookupArgs packageId version false false false
        |> execSuccessPkgChk output
        |> assertPackagesFound [ version ]

    [<Theory>]
    [<InlineData("zzz")>]
    let ``Lookup with incorrect package ID returns error`` packageId =

        runPkgChkLookupArgs packageId "" false false false |> execSysErrorPkgChk output

    [<Theory>]
    [<InlineData("xunit", "zzz")>]
    let ``Lookup with package ID and incorrect version returns error`` packageId version =

        runPkgChkLookupArgs packageId version false false false
        |> execSysErrorPkgChk output

    [<Theory>]
    [<InlineData("xunit", "2.9.3", "2.9.1")>]
    [<InlineData("System.Net.Http", "4.3.4", "4.3.0-preview1-24530-04")>]
    let ``Lookup with package ID  with prerelease returns all versions`` packageId version1 version2 =

        runPkgChkLookupArgs packageId "" true true false
        |> execSuccessPkgChk output
        |> assertPackagesFound [ version1; version2 ]

    [<Theory>]
    [<InlineData("xunit", "2.9.3", "2.5.2-pre.6")>]
    [<InlineData("System.Net.Http", "4.3.4", "4.3.0-preview1-24530-04")>]
    let ``Lookup with package ID  without prerelease returns all versions`` packageId version1 version2 =

        runPkgChkLookupArgs packageId "" true false false
        |> execSuccessPkgChk output
        |> assertPackagesFound [ version1 ]
        |> assertPackagesNotFound [ version2 ]

    [<Theory>]
    [<InlineData("xunit", false, "2.9.3")>]
    [<InlineData("xunit", true, "2.9.3")>]
    let ``Lookup with package ID returns deprecations`` packageId allVersions version1 =

        runPkgChkLookupArgs packageId version1 allVersions false false
        |> execSuccessPkgChk output
        |> assertPackagesFound [ version1 ]
        |> assertPackagesFound [ "Thisversionisdeprecated." ]

    [<Theory>]
    [<InlineData("System.Net.Http", false, "4.3.1")>]
    [<InlineData("System.Net.Http", true, "4.3.1")>]
    let ``Lookup with package ID returns vulnerabilities`` packageId allVersions version1 =

        runPkgChkLookupArgs packageId version1 allVersions false false
        |> execSuccessPkgChk output
        |> assertPackagesFound [ version1 ]
        |> assertPackagesFound [ "Thisversionhasknownvulnerabilities." ]

    [<Theory>]
    [<InlineData("refit", "11.2.0")>]
    let ``Lookup with pacckage ID returns automation`` packageId version1 =
        runPkgChkLookupArgs packageId version1 false false true
        |> execSuccessPkgChk output
        |> assertPackagesFound [ @"Packageautomationfound." ]

    [<Theory>]
    [<InlineData("Newtonsoft.Json", "13.0.4")>]
    let ``Lookup with pacckage ID returns no automation`` packageId version1 =
        runPkgChkLookupArgs packageId version1 false false true
        |> execSuccessPkgChk output
        |> assertPackagesFound [ @"Nopackageautomationfound." ]
