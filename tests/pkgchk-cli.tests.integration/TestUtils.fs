namespace pkgchk.tests.integration

open System
open System.Diagnostics
open FsUnit.Xunit
open Xunit.Abstractions

[<AutoOpen>]
module TestUtils =

    [<Literal>]
    let httpPackage = "System.Net.Http"

    [<Literal>]
    let regexPackage = "System.Text.RegularExpressions"

    [<Literal>]
    let aadPackage = "Microsoft.IdentityModel.Clients.ActiveDirectory"

    [<Literal>]
    let sysIoPackage = "System.IO"

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
        sprintf "dotnet add ./%s/testproj.csproj package %s -v 4.5.1" outDir aadPackage

    let addPackageDowngradeAadPackageArgs outDir =
        // conflicts with httpPackage - introduces 4.3.4
        sprintf "dotnet add ./%s/testproj.csproj package %s -v 5.3.0" outDir aadPackage

    let runPkgChkArgs outDir =
        sprintf
            "dotnet pkgchk-cli.dll scan ./%s/testproj.csproj --transitive true --deprecated true --trace --no-banner "
            outDir

    let runPkgChkDependenciesArgs outDir includeTransitives =
        sprintf
            "dotnet pkgchk-cli.dll list ./%s/testproj.csproj --transitive %b --trace --no-banner "
            outDir
            includeTransitives

    let runPkgChkUpgradesArgs outDir (breakOnHit: bool) (inclusions: seq<string>) (exclusions: seq<string>) =
        let inclusions =
            inclusions
            |> Seq.map (sprintf "--included-package %s")
            |> pkgchk.String.join " "

        let exclusions =
            exclusions
            |> Seq.map (sprintf "--excluded-package %s")
            |> pkgchk.String.join " "

        sprintf
            "dotnet pkgchk-cli.dll upgrades ./%s/testproj.csproj --no-banner --break-on-upgrades %b %s %s"
            outDir
            breakOnHit
            inclusions
            exclusions


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

    let logProcArgs (output: ITestOutputHelper) (proc: Process) =
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

    let logExecution (output: ITestOutputHelper) (rc, out, err) =
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

    let assertSuccessfulPkgChkNoOutput (rc, out, err) =
        rc |> should equal 0
        out |> should be NullOrEmptyString
        err |> should be NullOrEmptyString
        (rc, out, err)

    let assertFailedPkgChk (rc, out, err) =
        rc |> should equal 1
        out |> should not' (be NullOrEmptyString)
        err |> should be NullOrEmptyString
        (rc, out, err)

    let assertSysErrorPkgChk (rc, out, err) =
        rc |> should equal 2
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

    let execSuccess (output: ITestOutputHelper) =
        createProc
        >> logProcArgs output
        >> executeProc
        >> logExecution output
        >> assertSuccessfulExecution

    let execFailedPkgChk (output: ITestOutputHelper) =
        createProc
        >> logProcArgs output
        >> executeProc
        >> logExecution output
        >> assertFailedPkgChk

    let execSysErrorPkgChk (output: ITestOutputHelper) =
        createProc
        >> logProcArgs output
        >> executeProc
        >> logExecution output
        >> assertSysErrorPkgChk

    let execSuccessPkgChk (output: ITestOutputHelper) =
        createProc
        >> logProcArgs output
        >> executeProc
        >> logExecution output
        >> assertSuccessfulPkgChk

    let execSuccessPkgChkNoOutput (output: ITestOutputHelper) =
        createProc
        >> logProcArgs output
        >> executeProc
        >> logExecution output
        >> assertSuccessfulPkgChkNoOutput
