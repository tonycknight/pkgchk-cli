namespace pkgchk.tests

open System
open FsUnit.Xunit
open pkgchk.Markdown
open Xunit

module MarkdownTests =

    [<FsCheck.Xunit.Property(Arbitrary = [| typeof<AlphaNumericString> |], Verbose = true)>]
    let ``colourise projects colour & value`` (colour: string, value: string) =
        let result = colourise colour value
        result.IndexOf(colour) >= 0 
        && result.IndexOf(value) >= 0
    

    [<FsCheck.Xunit.Property(Arbitrary = [| typeof<AlphaNumericString> |], Verbose = true)>]
    let ``formatSeverityColour projects colour and value`` (value: string) =
        let result = formatSeverityColour value
        let colour = pkgchk.Rendering.severityColour value

        result.IndexOf(colour) >= 0 
        && result.IndexOf(value) >= 0

    [<FsCheck.Xunit.Property(Arbitrary = [| typeof<AlphaNumericString> |], Verbose = true)>]
    let ``formatReasonColour projects colour and value`` (value: string) =
        let result = formatReasonColour value
        let colour = pkgchk.Rendering.reasonColour value

        result.IndexOf(colour) >= 0 
        && result.IndexOf(value) >= 0
