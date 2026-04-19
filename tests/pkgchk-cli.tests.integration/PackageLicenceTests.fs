namespace pkgchk.tests.integration

open Xunit

type PackageLicenceTests(output: ITestOutputHelper) =

    let execSuccess = execSuccess output
    let execSuccessPkgChk = execSuccessPkgChk output
    let execFailedPkgChk = execFailedPkgChk output

    [<Fact>]
    let ``Project without packages returns empty list`` () =

        let outDir = getOutDir ()

        createProjectArgs outDir |> execSuccess

        runPkgChkLicenceArgs outDir false false [] []
        |> execSuccessPkgChk
        |> assertTitleShowsNoLicences

    [<Fact>]
    let ``Project with allowed licence returns empty list`` () =

        let outDir = getOutDir ()

        createProjectArgs outDir |> execSuccess
        addMitPackageArgs outDir |> execSuccess
        addApachePackageArgs outDir |> execSuccess

        runPkgChkLicenceArgs outDir false false [ "MIT"; "apache-2.0" ] []
        |> execSuccessPkgChk
        |> assertTitleShowsNoLicences

    [<Fact>]
    let ``Project with default allowed licence returns empty list`` () =

        let outDir = getOutDir ()

        createProjectArgs outDir |> execSuccess
        addMitPackageArgs outDir |> execSuccess
        addApachePackageArgs outDir |> execSuccess

        runPkgChkLicenceArgs outDir false false [] []
        |> execSuccessPkgChk
        |> assertTitleShowsNoLicences

    [<Fact>]
    let ``Project with disallowed licence returns non-empty list`` () =

        let outDir = getOutDir ()

        createProjectArgs outDir |> execSuccess
        addMitPackageArgs outDir |> execSuccess
        addApachePackageArgs outDir |> execSuccess

        runPkgChkLicenceArgs outDir false false [] [ "MIT"; "apache-2.0" ]
        |> execFailedPkgChk
        |> assertPackagesFound [ mitPackage; apachePackage ]

    [<Fact>]
    let ``Project with allowed disallowed licences returns non-empty list`` () =

        let outDir = getOutDir ()

        createProjectArgs outDir |> execSuccess
        addMitPackageArgs outDir |> execSuccess
        addApachePackageArgs outDir |> execSuccess

        runPkgChkLicenceArgs outDir false false [ "MIT" ] [ "apache-2.0" ]
        |> execFailedPkgChk
        |> assertPackagesFound [ apachePackage ]
        |> assertPackagesNotFound [ mitPackage ]

    [<Fact>]
    let ``Project with unknown licences returns non-empty list`` () =

        let outDir = getOutDir ()

        createProjectArgs outDir |> execSuccess
        addMitPackageArgs outDir |> execSuccess
        addApachePackageArgs outDir |> execSuccess

        runPkgChkLicenceArgs outDir false false [ "abc" ] [ "xyz" ]
        |> execFailedPkgChk
        |> assertPackagesFound [ apachePackage; mitPackage ]
        
    [<Fact>]
    let ``Project with unknown licences including missing licences returns non-empty list`` () =

        let outDir = getOutDir ()

        createProjectArgs outDir |> execSuccess
        addMitPackageArgs outDir |> execSuccess
        addApachePackageArgs outDir |> execSuccess

        runPkgChkLicenceArgs outDir false true [ "abc" ] [ "xyz" ]
        |> execFailedPkgChk
        |> assertPackagesFound [ apachePackage; mitPackage ]

    [<Fact>]
    let ``Project with missing licence returns empty list`` () =

        let outDir = getOutDir ()

        createProjectArgs outDir |> execSuccess
        addMissingLicencePackageArgs outDir |> execSuccess

        runPkgChkLicenceArgs outDir false true [] []
        |> execSuccessPkgChk
        |> assertTitleShowsNoLicences

    [<Fact>]
    let ``Project with missing licence returns non-empty list`` () =

        let outDir = getOutDir ()

        createProjectArgs outDir |> execSuccess
        addMissingLicencePackageArgs outDir |> execSuccess

        runPkgChkLicenceArgs outDir false false [] []
        |> execFailedPkgChk
        |> assertPackagesFound [ missingLicencePackage ]
