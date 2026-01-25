namespace pkgchk.tests.integration

open System
open Xunit

type VulnerablePackageTests(output: ITestOutputHelper) =

    let execSuccess = execSuccess output
    let execFailedPkgChk = execFailedPkgChk output

    [<Fact>]
    let ``Project with multiple vulnerable packages returns Error`` () =

        let outDir = getOutDir ()

        createProjectArgs outDir |> execSuccess

        addBadHttpPackageArgs outDir |> execSuccess

        addBadRegexPackageArgs outDir |> execSuccess

        runPkgChkArgs outDir
        |> execFailedPkgChk
        |> assertTitleShowsVulnerabilities
        |> assertPackagesFound [ httpPackage; regexPackage ]

    [<Fact>]
    let ``Project with mixed vulnerable / good packages returns Error`` () =

        let outDir = getOutDir ()

        createProjectArgs outDir |> execSuccess

        addGoodRegexPackageArgs outDir |> execSuccess

        addBadHttpPackageArgs outDir |> execSuccess

        runPkgChkArgs outDir
        |> execFailedPkgChk
        |> assertTitleShowsVulnerabilities
        |> assertPackagesFound [ httpPackage ]
        |> assertPackagesNotFound [ regexPackage ]
