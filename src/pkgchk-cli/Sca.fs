namespace pkgchk

module Sca =
    
    let restore (config: ScanConfiguration) projectPath logging =
        if config.noRestore then
            Choice1Of2 false
        else
            let runRestoreProcParse run proc =
                proc
                |> run
                |> (function
                | Choice2Of2 error -> Choice2Of2 error
                | _ -> Choice1Of2 true)

            projectPath
            |> ScaCommandArgs.restoreArgs
            |> Io.createProcess
            |> runRestoreProcParse (CliCommands.runProc logging)

    let scan (context: ScaScanContext) =
        ScaCommandArgs.scanArgs context
        |> Array.map (fun (args, parser) -> (Io.createProcess args, parser))
        |> Array.map (fun (proc, parser) ->
            match proc |> (CliCommands.runProc context.trace) with
            | Choice1Of2 json -> parser json
            | Choice2Of2 x -> Choice2Of2 x)

    