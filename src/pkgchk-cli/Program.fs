namespace pkgchk

open System.Diagnostics.CodeAnalysis
open Spectre.Console
open Spectre.Console.Cli

[<ExcludeFromCodeCoverage>]
module Program =

    let console = Spectre.Console.AnsiConsole.MarkupLine 

    [<EntryPoint>]
    let main argv =
        let app =
            CommandApp<PackageCheckCommand>()
                .WithDescription("Check project dependency packages for vulnerabilities and deprecations.")

        app.Configure(fun c -> c.PropagateExceptions().ValidateExamples().TrimTrailingPeriods(false) |> ignore)

        try
            app.Run(argv)
        with ex ->
            ex.Message |> Console.error |> console
            ReturnCodes.sysError
