namespace pkgchk

open System
open System.Diagnostics
open System.Diagnostics.CodeAnalysis

[<ExcludeFromCodeCoverage>]
module Process =

    let createProcess args =
        let p = new Process()

        p.StartInfo.UseShellExecute <- false
        p.StartInfo.FileName <- "dotnet"
        p.StartInfo.Arguments <- args
        p.StartInfo.CreateNoWindow <- true
        p.StartInfo.WindowStyle <- ProcessWindowStyle.Hidden
        p.StartInfo.RedirectStandardError <- true
        p.StartInfo.RedirectStandardOutput <- true
        p.StartInfo.WorkingDirectory <- Environment.CurrentDirectory
        p

    let run log (proc: Process) =
        try
            try
                let sw = System.Diagnostics.Stopwatch.StartNew()
                let fmtOut = String.leading Int32.MaxValue >> String.escapeMarkup

                log $"Running command:{Environment.NewLine}{proc.StartInfo.FileName} {proc.StartInfo.Arguments}"

                if proc.Start() then
                    log "Getting response..."
                    let out = proc.StandardOutput.ReadToEnd()
                    let err = proc.StandardError.ReadToEnd()
                    proc.WaitForExit()

                    sw.Stop()
                    log $"Duration: {sw.ElapsedMilliseconds:N2}ms"
                    log $"ExitCode: {proc.ExitCode}"
                    log "stdout:"
                    out |> fmtOut |> log

                    log "stderr:"
                    err |> fmtOut |> log

                    if proc.ExitCode <> 0 then
                        log $"Non-zero exit code: {proc.ExitCode}"

                        $"Exit code {proc.ExitCode} from {proc.StartInfo.FileName} {proc.StartInfo.Arguments}{Environment.NewLine}{fmtOut out}"
                        |> Choice2Of2

                    else if (String.IsNullOrWhiteSpace(err)) then
                        log "Successfully fetched response."
                        Choice1Of2(out)
                    else
                        log "Error detected getting response."
                        Choice2Of2(err)
                else
                    Choice2Of2("Cannot start process")
            with ex ->
                Choice2Of2(ex.Message)
        finally
            proc.Dispose()