namespace pkgchk

open System

module Program =
    [<EntryPoint>]
    let main args =

        let path =
            match args with
            | [||] -> ""
            | [| p |] -> p
            | [| p; _ |] -> p

        let r = Sca.createProcess path true |> Sca.get

        match r with
        | Choice1Of2 json ->
            match Sca.parse json with
            | Choice1Of2 [] ->
                Console.Out.WriteLine("No vulnerabilities found!")
                0
            | Choice1Of2 hits ->
                Console.Out.WriteLine("Vulnerabilities found!")
                hits |> Sca.formatHits |> Console.Out.WriteLine
                1
            | Choice2Of2 error ->
                Console.Error.WriteLine error
                99
        | Choice2Of2 err ->
            Console.Error.WriteLine err
            99
