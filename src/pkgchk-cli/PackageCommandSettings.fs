namespace pkgchk

open System.ComponentModel
open System.Diagnostics.CodeAnalysis
open Spectre.Console.Cli

[<ExcludeFromCodeCoverage>]
type PackageCommandSettings() =
    inherit CommandSettings()

    [<CommandArgument(0, "[SOLUTION|PROJECT]")>]
    [<Description("The solution or project file to check.")>]
    [<DefaultValue("")>]
    member val ProjectPath = "" with get, set

    [<CommandOption("--trace")>]
    [<Description("Show detailed working and Nuget results.")>]
    [<DefaultValue(false)>]
    member val TraceLogging = false with get, set

    [<CommandOption("--no-restore")>]
    [<Description("Don't automatically restore packages.")>]
    [<DefaultValue(false)>]
    member val NoRestore = false with get, set

    [<CommandOption("--no-banner")>]
    [<Description("Don't show the banner.")>]
    [<DefaultValue(false)>]
    member val NoBanner = false with get, set
