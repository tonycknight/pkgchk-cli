namespace pkgchk.tests.integration

open System
open Xunit

type PackageDependencyTests(output: ITestOutputHelper) =

    let execSuccess = execSuccess output
    let execSuccessPkgChk = execSuccessPkgChk output
    let execFailedPkgChk = execFailedPkgChk output

    [<Fact>]
    let ``Project without transitives returns dependency list`` () =

        let outDir = getOutDir ()

        createProjectArgs outDir |> execSuccess

        addBadHttpPackageArgs outDir |> execSuccess

        addBadRegexPackageArgs outDir |> execSuccess

        runPkgChkDependenciesArgs outDir false
        |> execSuccessPkgChk
        |> assertPackagesFound [ httpPackage; regexPackage ]
        |> assertPackagesNotFound [ sysIoPackage ]

    [<Fact>]
    let ``Project with transitives returns dependency list`` () =

        let outDir = getOutDir ()

        createProjectArgs outDir |> execSuccess

        addBadHttpPackageArgs outDir |> execSuccess

        addBadRegexPackageArgs outDir |> execSuccess

        runPkgChkDependenciesArgs outDir true
        |> execSuccessPkgChk
        |> assertPackagesFound [ httpPackage; regexPackage ]
