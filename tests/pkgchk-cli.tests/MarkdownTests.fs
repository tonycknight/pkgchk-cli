namespace pkgchk.tests

open System
open FsUnit.Xunit
open pkgchk.Markdown
open Xunit

module MarkdownTests =

    [<FsCheck.Xunit.Property(Arbitrary = [| typeof<AlphaNumericString> |], Verbose = true)>]
    let ``colourise projects colour & value`` (colour: string, value: string) =
        let result = colourise colour value
        result.IndexOf(colour) >= 0 && result.IndexOf(value) >= 0


    [<FsCheck.Xunit.Property(Arbitrary = [| typeof<AlphaNumericString> |], Verbose = true)>]
    let ``formatSeverityColour projects colour and value`` (value: string) =
        let result = formatSeverityColour value
        let colour = pkgchk.Rendering.severityColour value

        result.IndexOf(colour) >= 0 && result.IndexOf(value) >= 0

    [<FsCheck.Xunit.Property(Arbitrary = [| typeof<KnownHitSeverity> |], Verbose = true)>]
    let ``formatSeverity projects emote, colour and value`` (value: string) =
        let result = formatSeverity value
        let colour = pkgchk.Rendering.severityColour value
        let emote = pkgchk.Rendering.severityEmote value

        result.IndexOf(colour) >= 0
        && result.IndexOf(emote) >= 0
        && result.IndexOf(value) >= 0

    [<FsCheck.Xunit.Property(Arbitrary = [| typeof<KnownHitReason> |], Verbose = true)>]
    let ``formatReasonColour projects colour and value`` (value: string) =
        let result = formatReasonColour value
        let colour = pkgchk.Rendering.reasonColour value

        result.IndexOf(colour) >= 0 && result.IndexOf(value) >= 0

    [<FsCheck.Xunit.Property(Arbitrary = [| typeof<AlphaNumericString> |], Verbose = true)>]
    let ``title returns appropriate title`` (hits: pkgchk.ScaHit list) =
        let result = title hits |> pkgchk.String.joinLines

        match hits with
        | [] -> result.Contains("No vulnerabilities found!")
        | xs -> result.Contains("Vulnerabilities found!")

    [<FsCheck.Xunit.Property(Arbitrary = [| typeof<AlphaNumericString> |], Verbose = true)>]
    let ``nugetLinkPkgVsn builds markdown link``(packageId: string, version: string)=
        let result = pkgchk.Markdown.nugetLinkPkgVsn packageId version
                
        result.StartsWith($"[{packageId}]")
        && result.EndsWith($"({pkgchk.Rendering.nugetLink (packageId, version)})")

    [<FsCheck.Xunit.Property(Arbitrary = [| typeof<AlphaNumericString> |], Verbose = true)>]
    let ``nugetLinkPkgSuggestion builds markdown link`` (packageId: string, suggestion: string)=
        let result = pkgchk.Markdown.nugetLinkPkgSuggestion packageId suggestion
        
        result.StartsWith($"[{suggestion}]")
        && result.EndsWith($"({pkgchk.Rendering.nugetLink (packageId, String.Empty)})")