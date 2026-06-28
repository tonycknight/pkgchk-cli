namespace pkgchk.reporting.tests

open System
open FsCheck
open FsCheck.Xunit
open pkgchk.reporting.Markdown

module MarkdownTests =

    [<Property(Arbitrary = [| typeof<AlphaNumericString> |], Verbose = true)>]
    let ``hdr has correct count of hashses`` (depth: PositiveInt) (value: string) =
        let prefix = new string('#', depth.Get)
        let r = hdr depth.Get value
        
        r.StartsWith($"{prefix} ")

    [<Property(Arbitrary = [| typeof<AlphaNumericString> |], Verbose = true)>]
    let ``hdr ends with string`` (depth: PositiveInt) (value: string) =
        let prefix = new string('#', depth.Get)
        let r = hdr depth.Get value
        
        r.EndsWith(value)

    [<Property(Arbitrary = [| typeof<AlphaNumericString> |], Verbose = true)>]
    let ``colourise projects colour & value`` (colour: string, value: string) =
        let result = colourise colour value
        result.IndexOf(colour) >= 0 && result.IndexOf(value) >= 0

    [<Property(Arbitrary = [| typeof<AlphaNumericString> |], Verbose = true)>]
    let ``image builds link`` (uri: string) =
        let result = pkgchk.reporting.Markdown.image uri

        result = $"![image]({uri})"

    [<Property(Arbitrary = [| typeof<AlphaNumericString> |], Verbose = true)>]
    let ``italic surrounded by underscores``(value: string) =
        let r = italic value;

        r.StartsWith("_") && r.EndsWith("_")

    [<Property(Arbitrary = [| typeof<AlphaNumericString> |], Verbose = true)>]
    let ``hyperlink builds formatted link`` (name: string) (uri: string) =
        let r = hyperlink name uri

        r.Contains($"[{name}]") && r.Contains($"({uri})")

    [<Property(Arbitrary = [| typeof<AlphaNumericString> |], Verbose = true)>]
    let ``link builds formatted link`` (uri: string) =
        let r = link uri

        r.Contains($"[{uri}]") && r.Contains($"({uri})")