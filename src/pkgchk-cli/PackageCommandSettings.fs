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

    [<CommandOption("-i|--included-package", IsHidden = false)>]
    [<Description("The name of a package to include in the scan. Multiple packages can be specified.")>]
    member val IncludedPackages: string[] = [||] with get, set

    [<CommandOption("-x|--excluded-package", IsHidden = false)>]
    [<Description("The name of a package to exclude from the scan. Multiple packages can be specified.")>]
    member val ExcludedPackages: string[] = [||] with get, set

    [<CommandOption("--config", IsHidden = false)>]
    [<Description("Configuration file path.")>]
    [<DefaultValue("")>]
    member val ConfigFile = "" with get, set

    [<CommandOption("-o|--output")>]
    [<Description("Output directory for reports.")>]
    [<DefaultValue("")>]
    member val OutputDirectory = "" with get, set

    [<CommandOption("-f|--format")>]
    [<Description("Report formats. Valid forms are 'Markdown' or 'Json'. Multiple formats can be specified.")>]
    [<DefaultValue([| ReportFormat.Markdown |])>]
    member val ReportFormats: ReportFormat[] = [||] with get, set

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

    override this.Validate() : Spectre.Console.ValidationResult =
        let getErrors () =
            seq {
                if String.isNotEmpty this.GithubPrId then
                    if String.isEmpty this.GithubToken then
                        yield "Missing Github token."

                    if String.isEmpty this.GithubRepo then
                        yield "Missing Github repository. Use the form <owner>/<name>."

                    let repo = String.split '/' this.GithubRepo

                    if repo |> fst |> String.isEmpty then
                        yield "The repository owner is missing. Use the form <owner>/<name>."

                    if repo |> snd |> String.isEmpty then
                        yield "The repository name is missing. Use the form <owner>/<name>."

                    if String.isInt this.GithubPrId |> not then
                        yield "The PR ID must be an integer."
            }

        match getErrors () |> Array.ofSeq with
        | [||] -> base.Validate()
        | msgs ->
            let msg = msgs |> String.join System.Environment.NewLine
            Spectre.Console.ValidationResult.Error msg

[<ExcludeFromCodeCoverage>]
type PackageScanCommandSettings() =
    inherit PackageGithubCommandSettings()

    [<CommandOption("-v|--vulnerable")>]
    [<Description("Toggle vulnerable package checks. true to include them, false to exclude.")>]
    [<DefaultValue(true)>]
    member val IncludeVulnerables = true with get, set

    [<CommandOption("-t|--transitive")>]
    [<Description("Toggle transitive package checks. true to include them, false to exclude.")>]
    [<DefaultValue(true)>]
    member val IncludeTransitives = true with get, set

    [<CommandOption("-d|--deprecated")>]
    [<Description("Check deprecated packagess. true to include, false to exclude.")>]
    [<DefaultValue(false)>]
    member val IncludeDeprecations = false with get, set

    [<CommandOption("-s|--severity")>]
    [<Description("Severity levels to scan for. Matches will return non-zero exit codes. Multiple levels can be specified.")>]
    [<DefaultValue([| "High"; "Critical"; "Critical Bugs"; "Legacy" |])>]
    member val SeverityLevels: string array = [||] with get, set

[<ExcludeFromCodeCoverage>]
type PackageListCommandSettings() =
    inherit PackageGithubCommandSettings()

    [<CommandOption("-t|--transitive")>]
    [<Description("Toggle transitive package checks. true to include them, false to exclude.")>]
    [<DefaultValue(true)>]
    member val IncludeTransitives = true with get, set

[<ExcludeFromCodeCoverage>]
type PackageUpgradeCommandSettings() =
    inherit PackageGithubCommandSettings()

    [<CommandOption("--break-on-upgrades")>]
    [<Description("Break on outstanding package upgrades.")>]
    [<DefaultValue(false)>]
    member val BreakOnUpgrades = false with get, set
