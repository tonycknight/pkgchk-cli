namespace pkgchk.tests

open System
open System.Diagnostics
open FsUnit.Xunit
open Xunit
open Xunit.Abstractions

type IntegrationTests(output: ITestOutputHelper) =

    [<Literal>]
    let httpPackage = "System.Net.Http"

    [<Literal>]
    let regexPackage = "System.Text.RegularExpressions"

    [<Literal>]
    let aadPackage = "Microsoft.IdentityModel.Clients.ActiveDirectory"

    let clean80Columns (value: string) =
        value.Replace(" ", "").Replace(Environment.NewLine, "")

    let cmdArgs (cmd: string) =
        let x = cmd.IndexOf(' ')

        if x > 0 then
            (cmd.Substring(0, x), cmd.Substring(x + 1))
        else
            (cmd, "")

    let getOutDir () =
        System.Guid.NewGuid().ToString().Replace("-", "")

    let createProjectArgs outDir =
        sprintf "dotnet new classlib -o ./%s -n testproj" outDir

    let addBadHttpPackageArgs outDir =
        sprintf "dotnet add ./%s/testproj.csproj package %s -v 4.3.0" outDir httpPackage

    let addGoodHttpPackageArgs outDir =
        sprintf "dotnet add ./%s/testproj.csproj package %s -v 4.3.4" outDir httpPackage

    let addBadRegexPackageArgs outDir =
        sprintf "dotnet add ./%s/testproj.csproj package %s -v 4.3.0" outDir regexPackage

    let addGoodRegexPackageArgs outDir =
        sprintf "dotnet add ./%s/testproj.csproj package %s -v 4.3.1" outDir regexPackage

    let addDeprecatedAadPackageArgs outDir =
        sprintf "dotnet add ./%s/testproj.csproj package %s -v 5.3.0" outDir aadPackage

    let runPkgChkArgs outDir =
        sprintf "dotnet pkgchk-cli.dll ./%s/testproj.csproj --transitive true --deprecated true --trace " outDir

    let runPkgChkSeverityArgs outDir (severities: seq<string>) =
        let severityArgs =
            severities |> Seq.map (sprintf "--severity %s") |> pkgchk.String.join " "

        $"{runPkgChkArgs outDir} {severityArgs}"

    let createProc cmd =
        let (exec, args) = cmdArgs cmd
        let proc = new Process()
        proc.StartInfo.UseShellExecute <- false
        proc.StartInfo.FileName <- exec
        proc.StartInfo.Arguments <- args
        proc.StartInfo.CreateNoWindow <- true
        proc.StartInfo.WindowStyle <- ProcessWindowStyle.Hidden
        proc.StartInfo.RedirectStandardError <- true
        proc.StartInfo.RedirectStandardOutput <- true
        proc

    let logProcArgs (proc: Process) =
        $"Running command:{Environment.NewLine}{proc.StartInfo.FileName} {proc.StartInfo.Arguments}"
        |> output.WriteLine

        proc

    let executeProc (proc: Process) =
        let ok = proc.Start()

        if not ok then
            failwith "Cannot start process"

        let out = proc.StandardOutput.ReadToEnd()
        let err = proc.StandardError.ReadToEnd()
        proc.WaitForExit()

        (proc.ExitCode, out, err)

    let logExecution (rc, out, err) =
        if out <> "" then
            $"Output:{Environment.NewLine}{out}" |> output.WriteLine

        if err <> "" then
            $"Error:{Environment.NewLine}{out}" |> output.WriteLine

        (rc, out, err)

    let assertSuccessfulExecution (rc, out, err) =
        rc |> should equal 0
        out |> should not' (be NullOrEmptyString)
        err |> should be NullOrEmptyString

    let assertSuccessfulPkgChk (rc, out, err) =
        rc |> should equal 0
        out |> should not' (be NullOrEmptyString)
        err |> should be NullOrEmptyString
        (rc, out, err)

    let assertFailedPkgChk (rc, out, err) =
        rc |> should equal 1
        out |> should not' (be NullOrEmptyString)
        err |> should be NullOrEmptyString
        (rc, out, err)

    let assertTitleShowsVulnerabilities (rc, out, err) =
        out |> should haveSubstring "Vulnerabilities found!"
        (rc, out, err)

    let assertTitleShowsNoVulnerabilities (rc, out, err) =
        out |> should haveSubstring "No vulnerabilities found!"
        (rc, out, err)

    let assertPackagesFound (hits: string list) (rc, out, err) =
        hits |> Seq.iter (fun h -> out |> clean80Columns |> should haveSubstring h)
        (rc, out, err)

    let assertPackagesNotFound (misses: string list) (rc, out, err) =
        misses
        |> Seq.iter (fun h -> out |> clean80Columns |> should not' (haveSubstring h))

        (rc, out, err)

    let execSuccess =
        createProc
        >> logProcArgs
        >> executeProc
        >> logExecution
        >> assertSuccessfulExecution

    let execFailedPkgChk =
        createProc >> logProcArgs >> executeProc >> logExecution >> assertFailedPkgChk

    let execSuccessPkgChk =
        createProc
        >> logProcArgs
        >> executeProc
        >> logExecution
        >> assertSuccessfulPkgChk

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
