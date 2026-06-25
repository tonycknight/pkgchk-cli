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

    let renderReportLines (reportFiles: string list) =
        let msg =
            reportFiles
            |> List.map (fun f -> $"[link={f}]{f}[/]")
            |> String.joinPretty ", " " & "

        $"{System.Environment.NewLine}Report file(s) {msg} built."
        |> italic
        |> console

    let returnCode isSuccess =
        match isSuccess with
        | true -> ReturnCodes.validationOk
        | _ -> ReturnCodes.validationFailed
