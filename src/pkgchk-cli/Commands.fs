namespace pkgchk

module Commands =

    let private runProc logging proc =
        try
            proc |> Io.run logging
        finally
            proc.Dispose()

    let console = Spectre.Console.AnsiConsole.MarkupLine

    let trace traceLogging =
        if traceLogging then Console.grey >> console else ignore

    let getErrors procResults =
        procResults
        |> Seq.map (function
            | Choice2Of2 x -> x
            | _ -> "")
        |> Seq.filter String.isNotEmpty
        |> Seq.distinct

    let returnError error =
        error |> Console.error |> console
        ReturnCodes.sysError

    let renderTables (values: seq<Spectre.Console.Table>) =
        values |> Seq.iter Spectre.Console.AnsiConsole.Write

    let liftHits procResults =
        procResults
        |> Seq.collect (function
            | Choice1Of2 xs -> xs
            | _ -> [])
        |> List.ofSeq

    let sortHits (hits: seq<ScaHit>) =
        hits
        |> Seq.sortBy (fun h ->
            ((match h.kind with
              | ScaHitKind.Vulnerability -> 0
              | ScaHitKind.Dependency -> 1
              | ScaHitKind.VulnerabilityTransitive -> 2
              | ScaHitKind.Deprecated -> 3
              | ScaHitKind.DependencyTransitive -> 4),
             h.packageId))

    let getHits x = x |> liftHits |> sortHits |> List.ofSeq

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
            |> runRestoreProcParse (runProc logging)

    let scan (context: ScaScanContext) =
        Sca.scanArgs context
        |> Array.map (fun (args, parser) -> (Io.createProcess args, parser))
        |> Array.map (fun (proc, parser) ->
            match proc |> (runProc context.trace) with
            | Choice1Of2 json -> parser json
            | Choice2Of2 x -> Choice2Of2 x)
