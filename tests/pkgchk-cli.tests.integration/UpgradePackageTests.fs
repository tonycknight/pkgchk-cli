namespace pkgchk.tests.integration

open Xunit
open Xunit.Abstractions

type UpgradePackageTests(output: ITestOutputHelper) =

    let execSuccess = execSuccess output
    let execSuccessProc = execSuccessPkgChk output
    let execFailedProc = execFailedPkgChk output

    [<Fact>]
    let ``Project with outstanding upgrades returns upgrade list`` () =

        let outDir = getOutDir ()

        createProjectArgs outDir |> execSuccess

        addBadHttpPackageArgs outDir |> execSuccess
        addBadRegexPackageArgs outDir |> execSuccess

        runPkgChkUpgradesArgs outDir false [] []
        |> execSuccessProc
        |> assertPackagesFound [ httpPackage ]

    [<Fact>]
    let ``Project with outstanding upgrades with exclusions returns empty list`` () =

        let outDir = getOutDir ()

        createProjectArgs outDir |> execSuccess

        addBadHttpPackageArgs outDir |> execSuccess
        addBadRegexPackageArgs outDir |> execSuccess

        runPkgChkUpgradesArgs outDir false [] [ httpPackage ]
        |> execSuccessProc
        |> assertPackagesNotFound [ httpPackage ]
        |> assertPackagesFound [ regexPackage ]

    [<Fact>]
    let ``Project with outstanding upgrades with inclusions returns empty list`` () =

        let outDir = getOutDir ()

        createProjectArgs outDir |> execSuccess

        addBadHttpPackageArgs outDir |> execSuccess
        addBadRegexPackageArgs outDir |> execSuccess

        runPkgChkUpgradesArgs outDir false [ regexPackage ] []
        |> execSuccessProc
        |> assertPackagesFound [ regexPackage ]
        |> assertPackagesNotFound [ httpPackage ]

    [<Fact>]
    let ``Project with outstanding upgrades with inclusions breaks on error`` () =

        let outDir = getOutDir ()

        createProjectArgs outDir |> execSuccess

        addBadHttpPackageArgs outDir |> execSuccess
        addBadRegexPackageArgs outDir |> execSuccess

        runPkgChkUpgradesArgs outDir true [ regexPackage ] []
        |> execFailedProc
        |> assertPackagesFound [ regexPackage ]
        |> assertPackagesNotFound [ httpPackage ]

    [<Fact>]
    let ``Project with no dependencies does not break on error`` () =

        let outDir = getOutDir ()

        createProjectArgs outDir |> execSuccess

        runPkgChkUpgradesArgs outDir true [] []
        |> execSuccessProc
        |> assertPackagesNotFound [ httpPackage; regexPackage ]


    [<Fact>]
    let ``Project with outstanding upgrades with all excluded does not break on error`` () =

        let outDir = getOutDir ()

        createProjectArgs outDir |> execSuccess

        addBadHttpPackageArgs outDir |> execSuccess
        addBadRegexPackageArgs outDir |> execSuccess

        runPkgChkUpgradesArgs outDir true [] [ httpPackage; regexPackage ]
        |> execSuccessProc
        |> assertPackagesNotFound [ httpPackage; regexPackage ]
