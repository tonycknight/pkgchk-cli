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

    [<CommandOption("--config", IsHidden = false)>]
    [<Description("Configuration file path.")>]
    [<DefaultValue("")>]
    member val ConfigFile = "" with get, set

[<ExcludeFromCodeCoverage>]
type PackageGithubCommandSettings() =
    inherit PackageCommandSettings()

    [<CommandOption("--github-token", IsHidden = true)>]
    [<Description("A Github token.")>]
    [<DefaultValue("")>]
    member val GithubToken = "" with get, set

    [<CommandOption("--github-repo", IsHidden = true)>]
    [<Description("The name of the Github repository in the form <owner>/<repo>, e.g. github/octokit.")>]
    [<DefaultValue("")>]
    member val GithubRepo = "" with get, set

    [<CommandOption("--github-title", IsHidden = true)>]
    [<Description("The Github report title.")>]
    [<DefaultValue("")>]
    member val GithubSummaryTitle = "" with get, set

    [<CommandOption("--github-pr", IsHidden = true)>]
    [<Description("Pull request ID.")>]
    [<DefaultValue("")>]
    member val GithubPrId = "" with get, set

    [<CommandOption("--github-commit", IsHidden = true)>]
    [<Description("Commit hash.")>]
    [<DefaultValue("")>]
    member val GithubCommit = "" with get, set

    [<CommandOption("--pass-img", IsHidden = true)>]
    [<Description("URI of an image for successful scans.")>]
    [<DefaultValue("")>]
    member val GoodImageUri = "" with get, set

    [<CommandOption("--fail-img", IsHidden = true)>]
    [<Description("URI of an image for failed scans.")>]
    [<DefaultValue("")>]
    member val BadImageUri = "" with get, set