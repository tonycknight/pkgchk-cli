namespace pkgchk

open System.Diagnostics.CodeAnalysis
open Spectre.Console
open Spectre.Console.Cli

[<ExcludeFromCodeCoverage>]
module Program =
    let send = Console.send AnsiConsole.Console

    [<EntryPoint>]
    let main argv =
        let app = CommandApp<PackageCheckCommand>()

        app.Configure(fun c -> c.PropagateExceptions().ValidateExamples() |> ignore)

        try
            app.Run(argv)
        with ex ->
            ex.Message |> Console.error |> send
            Console.sysError
