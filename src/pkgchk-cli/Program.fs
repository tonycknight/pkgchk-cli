namespace pkgchk

open Spectre.Console.Cli

module Program =
    [<EntryPoint>]
    let main argv =
        let app = CommandApp<PackageCheckCommand>()

        app.Configure(fun c -> c.PropagateExceptions().ValidateExamples() |> ignore)

        app.Run(argv)
