namespace pkgchk.tests

open System
open System.Diagnostics
open FsUnit.Xunit
open Xunit

module IntegrationTests =
    
    let getOutDir () =
        System.Guid.NewGuid().ToString().Replace("-", "")

    let createProjectArgs outDir =
        sprintf "dotnet new classlib -o ./%s -n testproj" outDir

    let addBadHttpPackageArgs outDir =
        sprintf "dotnet add ./%s/testproj.csproj package System.Net.Http -v 4.3.0" outDir

    let addGoodHttpPackageArgs outDir =
        sprintf "dotnet add ./%s/testproj.csproj package System.Net.Http -v 4.3.4" outDir

    let addGoodRegexPackageArgs outDir =
        sprintf "dotnet add ./%s/testproj.csproj package System.Text.RegularExpressions -v 4.3.1" outDir

    let runPkgChkArgs outDir =
        sprintf "pkgchk-cli.exe ./%s/testproj.csproj -t" outDir
            
    let createProc cmd =
        let proc = new Process()
        proc.StartInfo.UseShellExecute <- false
        proc.StartInfo.RedirectStandardOutput <- true
        proc.StartInfo.FileName <- "cmd.exe"
        proc.StartInfo.Arguments <- sprintf "/C %s" cmd
        proc.StartInfo.CreateNoWindow <- true
        proc.StartInfo.WindowStyle <- ProcessWindowStyle.Hidden
        proc.StartInfo.RedirectStandardError <- true
        proc.StartInfo.RedirectStandardOutput <- true
        proc

    let executeProc (proc: Process) =
        let ok = proc.Start()

        if not ok then
            failwith "Cannot start process"

        let out = proc.StandardOutput.ReadToEnd()
        let err = proc.StandardError.ReadToEnd()
        proc.WaitForExit()

        (proc.ExitCode, out, err)

    let assertSuccessfulExecution (rc, out, err) =
        rc |> should equal 0
        out |> should not' (be NullOrEmptyString)
        err |> should be NullOrEmptyString

    let assertFailedPkgChk (rc, out, err) =
        rc |> should equal 1
        out |> should not' (be NullOrEmptyString)
        err |> should be NullOrEmptyString
            
    let execSuccess = createProc >> executeProc >> assertSuccessfulExecution

    let execFailedPkgChk = createProc >> executeProc >> assertFailedPkgChk

    [<Fact>]
    let ``Vanilla project returns OK`` () =

        let outDir = getOutDir ()

        createProjectArgs outDir |> execSuccess

        runPkgChkArgs outDir |> execSuccess

    [<Fact>]
    let ``Project with vulnerable package returns Error`` () =

        let outDir = getOutDir ()

        createProjectArgs outDir |> execSuccess

        addBadHttpPackageArgs outDir |> execSuccess

        runPkgChkArgs outDir |> execFailedPkgChk


    [<Fact>]
    let ``Project with good package returns OK`` () =

        let outDir = getOutDir ()

        createProjectArgs outDir |> execSuccess

        addGoodHttpPackageArgs outDir |> execSuccess

        runPkgChkArgs outDir |> execSuccess

    [<Fact>]
    let ``Project with mixed vulnerable / good packages returns Error`` () =

        let outDir = getOutDir ()

        createProjectArgs outDir |> execSuccess

        addGoodRegexPackageArgs outDir |> execSuccess

        addBadHttpPackageArgs outDir |> execSuccess

        runPkgChkArgs outDir |> execFailedPkgChk
