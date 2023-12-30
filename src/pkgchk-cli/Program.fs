namespace pkgchk

open System.Diagnostics.CodeAnalysis
open Spectre.Console.Cli

[<ExcludeFromCodeCoverage>]
module Program =
    [<EntryPoint>]
    let main argv =
        let app = CommandApp<PackageCheckCommand>()

        app.Configure(fun c -> c.PropagateExceptions().ValidateExamples() |> ignore)

        try
            app.Run(argv)
        with ex ->
            ex.Message |> Console.returnError Spectre.Console.AnsiConsole.Console
