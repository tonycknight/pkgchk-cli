namespace pkgchk.tests

open System
open FsCheck.Xunit
open pkgchk.Markdown

module MarkdownTests =

    [<Property(Arbitrary = [| typeof<AlphaNumericString> |], Verbose = true)>]
    let ``colourise projects colour & value`` (colour: string, value: string) =
        let result = colourise colour value
        result.IndexOf(colour) >= 0 && result.IndexOf(value) >= 0


    [<Property(Arbitrary = [| typeof<AlphaNumericString> |], Verbose = true)>]
    let ``formatSeverityColour projects colour and value`` (value: string) =
        let result = formatSeverityColour value
        let colour = pkgchk.Rendering.severityColour value

        result.IndexOf(colour) >= 0 && result.IndexOf(value) >= 0

    [<Property(Arbitrary = [| typeof<KnownHitSeverity> |], Verbose = true)>]
    let ``formatSeverity projects emote, colour and value`` (value: string) =
        let result = formatSeverity value
        let colour = pkgchk.Rendering.severityColour value
        let emote = pkgchk.Rendering.severityEmote value

        result.IndexOf(colour) >= 0
        && result.IndexOf(emote) >= 0
        && result.IndexOf(value) >= 0

    [<Property(Arbitrary = [| typeof<KnownHitReason> |], Verbose = true)>]
    let ``formatReasonColour projects colour and value`` (value: string) =
        let result = formatReasonColour value
        let colour = pkgchk.Rendering.reasonColour value

        result.IndexOf(colour) >= 0 && result.IndexOf(value) >= 0

    [<Property(Arbitrary = [| typeof<AlphaNumericString> |], Verbose = true)>]
    let ``title returns appropriate title`` (hits: pkgchk.ScaHit list) =
        let result = titleScan hits |> pkgchk.String.joinLines

        match hits with
        | [] -> result.Contains("No vulnerabilities found!")
        | xs -> result.Contains("Vulnerabilities found!")

    [<Property(Arbitrary = [| typeof<AlphaNumericString> |], Verbose = true)>]
    let ``nugetLinkPkgVsn builds markdown link`` (packageId: string, version: string) =
        let result = pkgchk.Markdown.nugetLinkPkgVsn packageId version

        result.StartsWith($"[{packageId} {version}]")
        && result.EndsWith($"({pkgchk.Rendering.nugetLink (packageId, version)})")

    [<Property(Arbitrary = [| typeof<AlphaNumericString> |], Verbose = true)>]
    let ``nugetLinkPkgSuggestion builds markdown link`` (packageId: string, suggestion: string) =
        let result = pkgchk.Markdown.nugetLinkPkgSuggestion packageId suggestion

        result.StartsWith($"[{suggestion}]")
        && result.EndsWith($"({pkgchk.Rendering.nugetLink (packageId, String.Empty)})")

    [<Property(Arbitrary = [| typeof<AlphaNumericString> |], Verbose = true)>]
    let ``imgLink biulds link`` (uri: string) =
        let result = pkgchk.Markdown.imgLink uri

        result = $"![image]({uri})"

    [<Property(Arbitrary = [| typeof<AlphaNumericString> |], Verbose = true)>]
    let ``pkgFramework renders HTML colour markup`` (hit: pkgchk.ScaHit) =
        let result = pkgchk.Markdown.pkgFramework hit
        
        result <> ""
        && result.StartsWith("<span style='color:#")
        && result.EndsWith("</span>")
        && result.Contains(hit.framework)

    [<Property(Arbitrary = [| typeof<KnownHitSeverity> |], Verbose = true)>]
    let ``formatSeverities renders severities``  (severities: string[] ) =
        let result = pkgchk.Markdown.formatSeverities severities
        
        severities |> Seq.forall result.Contains

    [<Property(Arbitrary = [| typeof<AlphaNumericString> |], MaxTest = 1)>]
    let ``footer renders`` () =
        let result = pkgchk.Markdown.footer |> pkgchk.String.joinLines

        result <> ""
        && result.Contains("Thank you")
        && result.Contains(pkgchk.App.packageId.ToLower())
        && result.Contains(pkgchk.App.repo)

    [<Property(Arbitrary = [| typeof<AlphaNumericString> |], Verbose = true)>]
    let ``titleUpgrades renders`` (hits: pkgchk.ScaHit list) =
        let result = pkgchk.Markdown.titleUpgrades hits |> pkgchk.String.joinLines

        result <> ""

    [<Property(Arbitrary = [| typeof<AlphaNumericString> |], Verbose = true)>]
    let ``formatHitCounts renders ``  (hits: pkgchk.ScaHitSummary[] ) =
        let severities = hits |> Seq.map (fun h -> h.severity) |> Seq.distinct |> Array.ofSeq

        let result = pkgchk.Markdown.formatHitCounts (severities, hits) |> pkgchk.String.joinLines

        match hits with
        | [||] -> result = ""
        | xs -> result <> ""
                && severities |> Seq.forall result.Contains
                
    