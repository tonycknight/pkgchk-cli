namespace pkgchk.tests

open System
open FsUnit.Xunit
open pkgchk.Console
open Xunit

module ConsoleTests =

    let rowCellsAsMarkup (row: Spectre.Console.TableRow) =
        row |> Seq.map (fun r -> r :?> Spectre.Console.Markup)

    let markupsHaveContent (values: seq<Spectre.Console.Markup>) =
        values |> Seq.forall (fun v -> v.Length > 0)

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

    [<FsCheck.Xunit.Property(Arbitrary = [| typeof<AlphaNumericString> |], Verbose = true)>]
    let ``hitSummaryRow yields non-empty values`` (value: pkgchk.ScaHitSummary) =
        let result = pkgchk.Console.hitSummaryRow value

        result |> Array.exists (pkgchk.String.isNotEmpty >> not) |> not

    [<FsCheck.Xunit.Property(Arbitrary = [| typeof<AlphaNumericString> |], Verbose = true)>]
    let ``hitSummaryRow yields formatted values`` (value: pkgchk.ScaHitSummary) =
        let result = pkgchk.Console.hitSummaryRow value

        result |> Array.exists (fun r -> r = pkgchk.Rendering.formatHitKind value.kind)
        && result |> Array.exists (fun r -> r.IndexOf(value.severity) >= 0)
        && result |> Array.exists (fun r -> r.IndexOf(value.count.ToString()) >= 0)

    [<FsCheck.Xunit.Property(Arbitrary = [| typeof<AlphaNumericString> |], Verbose = true)>]
    let ``hitSummaryTable produces table containing row`` (value: pkgchk.ScaHitSummary) =
        let values = [ value ]

        let t = pkgchk.Console.hitSummaryTable values

        t.Rows.Count = 1
        && t.Columns.Count = 3
        // Markup objects do not expose their contents, hence we check for basic existence
        && t.Rows |> Seq.collect rowCellsAsMarkup |> markupsHaveContent

    [<FsCheck.Xunit.Property(Arbitrary = [| typeof<AlphaNumericString> |], Verbose = true)>]
    let ``title returns appropriate title``(hits: pkgchk.ScaHit list) =
        let result = pkgchk.Console.title hits |> pkgchk.String.joinLines

        match hits with
        | [] -> result.StartsWith("[lime]No vulnerabilities found.")
        | xs -> result.StartsWith("[red]Vulnerabilities found!")

    [<FsCheck.Xunit.Property(Arbitrary = [| typeof<AlphaNumericString> |], Verbose = true)>]
    let ``hitPackage produces nuget link``(hit: pkgchk.ScaHit) =        
        match pkgchk.Console.hitPackage hit |> List.ofSeq with
        | [ h ] -> h.Contains($"https://www.nuget.org/packages/{hit.packageId}/{hit.resolvedVersion}")
        | _ -> false

    [<FsCheck.Xunit.Property(Arbitrary = [| typeof<AlphaNumericString> |], Verbose = true)>]
    let ``hitAdvisory produces appropriate rows``(hit: pkgchk.ScaHit) =
        let result = pkgchk.Console.hitAdvisory hit |> List.ofSeq

        match result with
        | [ r ] -> r.Contains(hit.advisoryUri)
        | _ -> false

    [<FsCheck.Xunit.Property(Arbitrary = [| typeof<AlphaNumericString> |], Verbose = true)>]
    let ``hitAdvisory on empty advisoryUri produces empty set``(hit: pkgchk.ScaHit) =
        let result = hit |> (fun h -> { h with advisoryUri = "" } ) |> pkgchk.Console.hitAdvisory |> List.ofSeq

        result |> Seq.isEmpty
