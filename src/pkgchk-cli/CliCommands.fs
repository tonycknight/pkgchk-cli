namespace pkgchk

module CliCommands =

    let console = Spectre.Console.AnsiConsole.MarkupLine

    let trace traceLogging =
        if traceLogging then Console.grey >> console else ignore

    let returnError error =
        error |> Console.error |> console
        ReturnCodes.sysError

    let renderTables (values: seq<Spectre.Console.Table>) =
        values |> Seq.iter Spectre.Console.AnsiConsole.Write

    let renderBanner (nuget: Tk.Nuget.INugetClient) = nuget |> App.banner |> console

    let renderReportLine reportFile =
        // TODO: transform to a sequence input & block
        $"{System.Environment.NewLine}Report file [link={reportFile}]{reportFile}[/] built."
        |> Console.italic
        |> console

    let returnCode isSuccess =
        match isSuccess with
        | true -> ReturnCodes.validationOk
        | _ -> ReturnCodes.validationFailed
