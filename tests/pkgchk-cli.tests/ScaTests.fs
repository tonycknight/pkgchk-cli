namespace pkgchk.tests

open FsUnit.Xunit
open pkgchk.Sca
open Xunit

module ScaTests =

    [<Theory>]
    [<InlineData("")>]
    [<InlineData(" a ")>]
    [<InlineData("x.csproj")>]
    let ``formatProject returns yellow`` proj =

        let r = formatProject proj

        r |> should haveSubstring "yellow"
        r |> should haveSubstring proj

    [<Theory>]
    [<InlineData("", "yellow")>]
    [<InlineData("Unknown", "yellow")>]
    [<InlineData("High", "red")>]
    [<InlineData("Critical", "italic red")>]
    [<InlineData("Moderate", "#d75f00")>]
    let ``formatSeverity returns colouration`` value expected =
        let r = formatSeverity value

        r |> should haveSubstring expected
