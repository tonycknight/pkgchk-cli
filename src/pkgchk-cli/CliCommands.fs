namespace pkgchk

open pkgchk.reporting.Console

module CliCommands =

    let console = Spectre.Console.AnsiConsole.MarkupLine

    let trace traceLogging =
        if traceLogging then grey >> console else ignore

    let returnError msg =
        msg |> error |> console
        ReturnCodes.sysError

    let renderTables (values: seq<Spectre.Console.Table>) =
        values |> Seq.iter Spectre.Console.AnsiConsole.Write

    let renderBanner (nuget: Tk.Nuget.INugetClient) = nuget |> App.banner |> console
    // TODO: remove
    let renderReportLines (reportFiles: string seq) =
        let msg =
            reportFiles
            |> Seq.map (fun f -> $"[link={f}]{f}[/]" |> cyan)
            |> String.joinPretty ", " " & "
        if msg.Length > 0 then
            $"{System.Environment.NewLine}Report file(s) {msg} built." |> italic |> console

    let returnCode isSuccess =
        match isSuccess with
        | true -> ReturnCodes.validationOk
        | _ -> ReturnCodes.validationFailed
