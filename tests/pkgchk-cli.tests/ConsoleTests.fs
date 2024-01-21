namespace pkgchk.tests

open FsUnit.Xunit
open pkgchk.Console
open Xunit

module ConsoleTests =
    
    let scrubScaHitSummary (value: pkgchk.ScaHitSummary) =
        // Scrub because:
        // - FsCheck can provide null strings
        // - FsCheck can provide unbalanced markup characters;
        let severity = (value.severity |> pkgchk.String.nullToEmpty).Replace("[", "").Replace("]", "")
        { value with severity = severity }

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
    let ``nugetLinkPkgVsn produces link`` (package: FsCheck.NonEmptyString, version: FsCheck.NonEmptyString) =
        let r = pkgchk.Console.nugetLinkPkgVsn package.Get version.Get
                
        r.IndexOf("https://www.nuget.org/packages") >= 0
        && r.IndexOf(package.Get) >= 0
        && r.IndexOf(version.Get) >= 0
        
    [<FsCheck.Xunit.Property>]
    let ``nugetLinkPkgSuggestion produces link`` (package: FsCheck.NonEmptyString, suggestion: FsCheck.NonEmptyString) =
        let r = pkgchk.Console.nugetLinkPkgSuggestion package.Get suggestion.Get
                
        r.IndexOf("https://www.nuget.org/packages") >= 0
        && r.IndexOf(package.Get) >= 0
        && r.IndexOf(suggestion.Get) >= 0
        

    [<Fact>]
    let ``table produces console table`` () =
        let t = pkgchk.Console.table ()

        t.Border |> should equal Spectre.Console.TableBorder.None
        t.ShowHeaders |> should equal false
        t.Columns.Count |> should equal 0
        t.Rows.Count |> should equal 0

    [<Fact>]
    let ``tableColumn produces columns`` () =
        let t = pkgchk.Console.table ()
        let t2 = t |> pkgchk.Console.tableColumn "name"

        t2.Columns.Count |> should equal 1
        t2 |> should equal t

    [<FsCheck.Xunit.Property>]
    let ``hitSummaryRow yields non-empty values`` (value: pkgchk.ScaHitSummary) =
        let result = pkgchk.Console.hitSummaryRow value

        result |> Array.exists (pkgchk.String.isNotEmpty >> not) |> not

    [<FsCheck.Xunit.Property>]
    let ``hitSummaryRow yields formatted values`` (value: pkgchk.ScaHitSummary) =        
        let value = scrubScaHitSummary value

        let result = pkgchk.Console.hitSummaryRow value

        result |> Array.exists (fun r -> r = pkgchk.Rendering.formatHitKind value.kind)
        && result |> Array.exists (fun r -> r.IndexOf(value.severity) >= 0)
        && result |> Array.exists (fun r -> r.IndexOf(value.count.ToString()) >= 0)

    [<FsCheck.Xunit.Property>]
    let ``hitSummaryTable produces table containing row`` (value: pkgchk.ScaHitSummary) =
        let value = scrubScaHitSummary value
        let values = [ value ]
        
        let t = pkgchk.Console.hitSummaryTable values
        
        t.Rows.Count = 1 &&
        t.Columns.Count = 3
        