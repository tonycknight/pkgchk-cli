namespace pkgchk

open System.Diagnostics.CodeAnalysis
open System.Reflection

[<ExcludeFromCodeCoverage>]
module App =
    let version () =
        Assembly
            .GetExecutingAssembly()
            .GetCustomAttributes<AssemblyInformationalVersionAttribute>()
        |> Seq.map _.InformationalVersion
        |> Seq.tryHead

    let repo = "https://github.com/tonycknight/pkgchk-cli"

    let banner () =
        seq {
            Console.cyan "Pkgchk-Cli"

            version ()
            |> Option.defaultValue "unknown"
            |> Console.yellow
            |> sprintf "Version %s"

            repo |> Console.cyan |> sprintf "For more information, see %s" |> Console.italic

            "Thank you for using my software" |> Console.grey |> Console.italic
        }
        |> String.joinLines
