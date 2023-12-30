namespace pkgchk

open System
open System.Diagnostics
open System.Diagnostics.CodeAnalysis
open System.IO

[<ExcludeFromCodeCoverage>]
module Io =

    let toFullPath (path: string) =
        if not <| Path.IsPathRooted(path) then
            let wd = Environment.CurrentDirectory

            Path.Combine(wd, path)
        else
            path

    let createProcess args =
        let p = new Process()

        p.StartInfo.UseShellExecute <- false
        p.StartInfo.RedirectStandardOutput <- true
        p.StartInfo.FileName <- "dotnet"
        p.StartInfo.CreateNoWindow <- true
        p.StartInfo.WindowStyle <- ProcessWindowStyle.Hidden
        p.StartInfo.RedirectStandardError <- true
        p.StartInfo.RedirectStandardOutput <- true
        p.StartInfo.WorkingDirectory <- Environment.CurrentDirectory
        p.StartInfo.Arguments <- args

        p

    let run (proc: Process) =
        try
            if proc.Start() then
                let out = proc.StandardOutput.ReadToEnd()
                let err = proc.StandardError.ReadToEnd()
                proc.WaitForExit()

                if (String.IsNullOrWhiteSpace(err)) then
                    Choice1Of2(out)
                else
                    Choice2Of2(err)
            else
                Choice2Of2("Cannot start process")
        with ex ->
            Choice2Of2(ex.Message)
