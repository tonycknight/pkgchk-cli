namespace pkgchk.tests

open FsUnit.Xunit
open pkgchk.Console
open Xunit

module ConsoleTests =

    [<Theory>]
    [<InlineData("")>]
    [<InlineData(" a ")>]
    [<InlineData("x.csproj")>]
    let ``formatProject returns yellow`` proj =

        let r = colouriseProject proj

        r |> should haveSubstring "yellow"
        r |> should haveSubstring proj

    [<Theory>]
    [<InlineData("", "yellow")>]
    [<InlineData("Unknown", "yellow")>]
    [<InlineData("High", "red")>]
    [<InlineData("Critical", "italic red")>]
    [<InlineData("Moderate", "#d75f00")>]
    let ``formatSeverity returns colouration`` value expected =
        let r = colouriseSeverity value

        r |> should haveSubstring expected

    [<FsCheck.Xunit.Property>]
    let ``hitSummaryRow yields non-empty values`` (value: pkgchk.ScaHitSummary) =
        let result = pkgchk.Console.hitSummaryRow value

        result |> Array.exists (pkgchk.String.isNotEmpty >> not) |> not

    [<FsCheck.Xunit.Property>]
    let ``hitSummaryRow yields formatted values`` (value: pkgchk.ScaHitSummary) =
        // FsCheck can provide null strings; ensuree they're not
        let value =
            { value with
                severity = pkgchk.String.nullToEmpty value.severity }

        let result = pkgchk.Console.hitSummaryRow value

        result |> Array.exists (fun r -> r = pkgchk.Rendering.formatHitKind value.kind)
        && result |> Array.exists (fun r -> r.IndexOf(value.severity) >= 0)
        && result |> Array.exists (fun r -> r.IndexOf(value.count.ToString()) >= 0)
