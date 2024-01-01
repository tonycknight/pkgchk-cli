namespace pkgchk.tests

open FsUnit.Xunit
open pkgchk.ScaArgs
open Xunit

module ScaArgsTests =

    
    [<Theory>]
    [<InlineData("", true)>]
    [<InlineData("", false)>]
    [<InlineData("test.csproj", true)>]
    [<InlineData("test.csproj", false)>]
    let ``Vulnerabilities with project``(project, transitives) =
        let r = scanVulnerabilities transitives project

        let expected = $" list {project} package  --vulnerable {includeTransitives transitives} --format json --output-version 1 "
        r |> should equal expected

    [<Theory>]
    [<InlineData("", true)>]
    [<InlineData("", false)>]
    [<InlineData("test.csproj", true)>]
    [<InlineData("test.csproj", false)>]
    let ``Deprecations with project``(project, transitives) =
        let r = scanDeprecations transitives project

        let expected = $" list {project} package  --deprecated {includeTransitives transitives} --format json --output-version 1 "
        r |> should equal expected