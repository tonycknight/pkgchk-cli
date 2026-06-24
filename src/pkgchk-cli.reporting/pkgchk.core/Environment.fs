namespace pkgchk

open System.Diagnostics.CodeAnalysis

module Environment =

    [<ExcludeFromCodeCoverage>]
    let isRunningGithub =
        System.Environment.GetEnvironmentVariable("GITHUB_ACTIONS") <> null

