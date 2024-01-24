namespace pkgchk

open System
open System.Diagnostics.CodeAnalysis
open System.Reflection
open Microsoft.Extensions.DependencyInjection
open Spectre.Console.Cli

[<ExcludeFromCodeCoverage>]
type TypeResolver (sp: IServiceProvider) =
    interface ITypeResolver with
        member _.Resolve t = sp.GetService(t)
    interface IDisposable with
        member _.Dispose() = 
            match sp with
            | :? IDisposable as d -> d.Dispose()
            | _ -> ignore 0

[<ExcludeFromCodeCoverage>]
type TypeRegistrar (svcs: IServiceCollection) =
    interface ITypeRegistrar with
        member _.Build() = new TypeResolver(svcs.BuildServiceProvider()) :> ITypeResolver
        member _.Register(t: Type, i: Type) = svcs.AddSingleton(t, i) |> ignore
        member _.RegisterInstance(t: Type, v: obj) = svcs.AddSingleton(t,v) |> ignore
        member _.RegisterLazy(t: Type, f: Func<obj>) = svcs.AddSingleton(t, (fun p -> f.Invoke())) |> ignore

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
                    
    let svcs () =
        new ServiceCollection() 
        |> Tk.Nuget.ServiceExtensions.AddNugetClient 
        
    let spectreServices () =
        new TypeRegistrar(svcs ())