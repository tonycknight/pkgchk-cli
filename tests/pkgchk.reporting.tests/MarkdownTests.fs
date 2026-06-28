namespace pkgchk.reporting.tests

open System
open FsCheck
open FsCheck.Xunit
open pkgchk.reporting.Markdown

module MarkdownTests =
    let genTable value rowCount colCount =
        let cols = [ 1 .. colCount ] |> List.map (fun x -> $"hdr{x}")
        let rows = [ 1 .. rowCount ] |> List.map (fun r -> [ 1 .. colCount ] |> List.map (fun c -> $"{value} {r}.{c}") )

        { pkgchk.reporting.Table.empty with columns = cols; rows = rows }

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

    [<Property(Arbitrary = [| typeof<AlphaNumericString> |], Verbose = true)>]
    let ``table generates expected row count`` (value: string) (rowCount: PositiveInt) (colCount: PositiveInt)=
        
        let t = genTable value rowCount.Get colCount.Get

        let result = table t

        let expectedNewLines = rowCount.Get + 2
        let actualNewLines = result.Split(Environment.NewLine, StringSplitOptions.None)

        expectedNewLines = actualNewLines.Length

    [<Property(Arbitrary = [| typeof<AlphaNumericString> |], Verbose = true)>]
    let ``table generates header row`` (value: string) (rowCount: PositiveInt) (colCount: PositiveInt)=
        
        let t = genTable value rowCount.Get colCount.Get

        let result = table t

        let rows = 
            result.Split(Environment.NewLine, StringSplitOptions.None)
            |> Seq.map (fun r -> r.Split('|', StringSplitOptions.RemoveEmptyEntries ||| StringSplitOptions.TrimEntries))
            |> Array.ofSeq

        rows.[0] = (Array.ofSeq t.columns)
        
    [<Property(Arbitrary = [| typeof<AlphaNumericString> |], Verbose = true)>]
    let ``table generates splitter row`` (value: string) (rowCount: PositiveInt) (colCount: PositiveInt)=
        
        let t = genTable value rowCount.Get colCount.Get

        let result = table t

        let rows = 
            result.Split(Environment.NewLine, StringSplitOptions.None)
            |> Seq.map (fun r -> r.Split('|', StringSplitOptions.RemoveEmptyEntries ||| StringSplitOptions.TrimEntries))
            |> Array.ofSeq

        rows.[1] |> Seq.forall(fun c -> c = "-" )


    [<Property(Arbitrary = [| typeof<AlphaNumericString> |], Verbose = true)>]
    let ``table generates expected columns`` (value: string) (rowCount: PositiveInt) (colCount: PositiveInt)=
        
        let t = genTable value rowCount.Get colCount.Get

        let result = table t

        let rows = 
            result.Split(Environment.NewLine, StringSplitOptions.None)
            |> Seq.map (fun r -> r.Split('|', StringSplitOptions.RemoveEmptyEntries ||| StringSplitOptions.TrimEntries))
            |> Array.ofSeq
        
        rows
        |> Seq.forall (fun r -> r.Length = colCount.Get)
        
    [<Property(Arbitrary = [| typeof<AlphaNumericString> |], Verbose = true)>]
    let ``table generates rows`` (value: string) (rowCount: PositiveInt) (colCount: PositiveInt)=
        
        let t = genTable value rowCount.Get colCount.Get

        let result = table t

        let rows = 
            result.Split(Environment.NewLine, StringSplitOptions.None)
            |> Seq.map (fun r -> r.Split('|', StringSplitOptions.RemoveEmptyEntries ||| StringSplitOptions.TrimEntries))
            |> Seq.skip 2
            |> Array.ofSeq
                    
        rows
        |> Seq.zip t.rows
        |> Seq.forall (fun (r1,r2) -> r1 = (List.ofSeq r2))
    
    [<Property(Arbitrary = [| typeof<AlphaNumericString> |], Verbose = true)>]
    let ``table generates sparse rows`` (value: string) (rowCount: PositiveInt) (colCount: PositiveInt)=
        
        let t = genTable value rowCount.Get colCount.Get
        let t = { t with rows = t.rows |> List.map (fun _ -> [value] ) } // truncate rows for a sparse table

        let result = table t

        let rows = 
            result.Split(Environment.NewLine, StringSplitOptions.None)
            |> Seq.map (fun r -> r.Split('|', StringSplitOptions.RemoveEmptyEntries))
            |> Array.ofSeq
        
        rows
        |> Seq.forall (fun r -> r.Length = colCount.Get)

    [<Property(Arbitrary = [| typeof<AlphaNumericString> |], Verbose = true)>]
    let ``table generates oversized rows`` (value: string) (rowCount: PositiveInt) (colCount: PositiveInt)=
        
        let t = genTable value rowCount.Get colCount.Get
        let t = { t with rows = t.rows |> List.map (fun _ -> List.init (colCount.Get * 2) (fun _ -> value )) } // truncate additional columns per row

        let result = table t

        let rows = 
            result.Split(Environment.NewLine, StringSplitOptions.None)
            |> Seq.map (fun r -> r.Split('|', StringSplitOptions.RemoveEmptyEntries))
            |> Array.ofSeq
        
        rows
        |> Seq.forall (fun r -> r.Length = colCount.Get)