namespace pkgchk.reporting.tests

open System
open FsCheck.Xunit
open pkgchk.reporting.Markdown

module MarkdownTests =

    [<Property(Arbitrary = [| typeof<AlphaNumericString> |], Verbose = true)>]
    let ``colourise projects colour & value`` (colour: string, value: string) =
        let result = colourise colour value
        result.IndexOf(colour) >= 0 && result.IndexOf(value) >= 0

    [<Property(Arbitrary = [| typeof<AlphaNumericString> |], Verbose = true)>]
    let ``image builds link`` (uri: string) =
        let result = pkgchk.reporting.Markdown.image uri

        result = $"![image]({uri})"
