namespace pkgchk.tests.integration

open Xunit

type DeprecatedPackageTests(output: ITestOutputHelper) =

    let execSuccess = execSuccess output
    let execSuccessPkgChk = execSuccessPkgChk output
    let execFailedPkgChk = execFailedPkgChk output
    let execSysErrorFailedPkgChk = execSysErrorPkgChk output


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


    [<Fact>]
    let ``Project with downgraded packages returns Error`` () =

        let outDir = getOutDir ()

        createProjectArgs outDir |> execSuccess

        addGoodRegexPackageArgs outDir |> execSuccess

        addBadHttpPackageArgs outDir |> execSuccess

        addPackageDowngradeAadPackageArgs outDir |> execSuccess

        [ "high"; "legacy" ] |> runPkgChkSeverityArgs outDir |> execFailedPkgChk
