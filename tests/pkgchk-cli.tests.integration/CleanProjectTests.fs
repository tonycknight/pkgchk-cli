namespace pkgchk.tests.integration

open System
open Xunit
open Xunit

type CleanProjectTests(output: ITestOutputHelper) =

    let execSuccess = execSuccess output
    let execSuccessPkgChk = execSuccessPkgChk output
    let execFailedPkgChk = execFailedPkgChk output

    [<Fact>]
    let ``Vanilla project returns OK`` () =

        let outDir = getOutDir ()

        createProjectArgs outDir |> execSuccess

        runPkgChkArgs outDir
        |> execSuccessPkgChk
        |> assertTitleShowsNoVulnerabilities
        |> assertPackagesNotFound [ httpPackage; regexPackage ]


    [<Fact>]
    let ``Project with multiple good packages returns OK`` () =

        let outDir = getOutDir ()

        createProjectArgs outDir |> execSuccess

        addGoodHttpPackageArgs outDir |> execSuccess

        addGoodRegexPackageArgs outDir |> execSuccess

        runPkgChkArgs outDir
        |> execSuccessPkgChk
        |> assertTitleShowsNoVulnerabilities
        |> assertPackagesNotFound [ httpPackage; regexPackage ]
