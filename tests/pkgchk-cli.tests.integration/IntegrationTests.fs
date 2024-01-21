namespace pkgchk.tests.integration

open System
open Xunit
open Xunit.Abstractions

type IntegrationTests(output: ITestOutputHelper) =

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
    let ``Project with multiple good packages returns OK`` () =

        let outDir = getOutDir ()

        createProjectArgs outDir |> execSuccess

        addGoodHttpPackageArgs outDir |> execSuccess

        addGoodRegexPackageArgs outDir |> execSuccess

        runPkgChkArgs outDir
        |> execSuccessPkgChk
        |> assertTitleShowsNoVulnerabilities
        |> assertPackagesNotFound [ httpPackage; regexPackage ]

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

    [<Fact>]
    let ``Project with multiple deprecated packages returns Error`` () =

        let outDir = getOutDir ()

        createProjectArgs outDir |> execSuccess

        addDeprecatedAadPackageArgs outDir |> execSuccess

        runPkgChkArgs outDir
        |> execFailedPkgChk
        |> assertTitleShowsVulnerabilities
        |> assertPackagesFound [ aadPackage ]


    [<Fact>]
    let ``Project with mixed vulnerable / good / deprecated packages returns Error`` () =

        let outDir = getOutDir ()

        createProjectArgs outDir |> execSuccess

        addGoodRegexPackageArgs outDir |> execSuccess

        addBadHttpPackageArgs outDir |> execSuccess

        addDeprecatedAadPackageArgs outDir |> execSuccess

        runPkgChkArgs outDir
        |> execFailedPkgChk
        |> assertTitleShowsVulnerabilities
        |> assertPackagesFound [ httpPackage; aadPackage ]
        |> assertPackagesNotFound [ regexPackage ]

    [<Fact>]
    let ``Project with mixed vulnerable / good / deprecated packages where not matching severity requirements returns Ok``
        ()
        =

        let outDir = getOutDir ()

        createProjectArgs outDir |> execSuccess

        addGoodRegexPackageArgs outDir |> execSuccess

        addBadHttpPackageArgs outDir |> execSuccess

        addDeprecatedAadPackageArgs outDir |> execSuccess

        [ "test" ]
        |> runPkgChkSeverityArgs outDir
        |> execSuccessPkgChk
        |> assertTitleShowsNoVulnerabilities
        |> assertPackagesFound [ httpPackage; aadPackage ]
        |> assertPackagesNotFound [ regexPackage ]


    [<Fact>]
    let ``Project with mixed vulnerable / good / deprecated packages when matching severity requirements returns Error``
        ()
        =

        let outDir = getOutDir ()

        createProjectArgs outDir |> execSuccess

        addGoodRegexPackageArgs outDir |> execSuccess

        addBadHttpPackageArgs outDir |> execSuccess

        addDeprecatedAadPackageArgs outDir |> execSuccess

        [ "high"; "legacy" ]
        |> runPkgChkSeverityArgs outDir
        |> execFailedPkgChk
        |> assertTitleShowsVulnerabilities
        |> assertPackagesFound [ httpPackage; aadPackage ]
        |> assertPackagesNotFound [ regexPackage ]
