namespace pkgchk

module CliCommands =

    let runProc logging proc =
        try
            proc |> Process.run logging
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

