namespace pkgchk

open System.Diagnostics.CodeAnalysis
open Spectre.Console.Cli


[<ExcludeFromCodeCoverage>]
module Program =

    let console = Spectre.Console.AnsiConsole.MarkupLine

    [<EntryPoint>]
    let main argv =

        let svcs = App.spectreServices ()

        let app = CommandApp(svcs)
        app.Configure(fun c -> 
                            c.PropagateExceptions().ValidateExamples().TrimTrailingPeriods(false) |> ignore
                            c.AddCommand<PackageScanCommand>("scan").WithDescription("Check project dependency packages for vulnerabilities and deprecations.") |> ignore                            
                        )

        try
            app.Run(argv)
        with ex ->
            ex.Message |> Console.error |> console
            ReturnCodes.sysError
