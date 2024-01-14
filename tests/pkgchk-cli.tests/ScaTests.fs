namespace pkgchk.tests

open System
open FsUnit.Xunit
open FsCheck
open FsCheck.Xunit

type Fact = Xunit.FactAttribute
type Theory = Xunit.TheoryAttribute
type InlineData = Xunit.InlineDataAttribute

module ScaTests =

    let getFile filename =
        let assembly = System.Reflection.Assembly.GetExecutingAssembly()
        let nspace = assembly.GetExportedTypes().[0].Namespace
        assembly.GetManifestResourceStream($"{nspace}.{filename}")

    [<Theory>]
    [<InlineData(" ")>]
    [<InlineData("ABC")>]
    let ``parse of plain text`` (text) =
        match text |> pkgchk.Sca.parse with
        | Choice2Of2 msg -> ignore 0
        | _ -> failwith "No error raised"

    [<Fact>]
    let ``parse for empty results`` () =

        use f = getFile "ScaSampleEmpty.json"

        use reader = new System.IO.StreamReader(f)

        let r = reader.ReadToEnd() |> pkgchk.Sca.parse

        match r with
        | Choice1Of2 xs ->
            match xs with
            | [] -> ignore 0
            | _ -> failwith "Unrecognised list returned"
        | _ -> failwith "No error raised"


    [<Fact>]
    let ``parse for vulnerabilities`` () =

        use f = getFile "ScaSampleWithVulnerabilities.json"

        use reader = new System.IO.StreamReader(f)

        let r = reader.ReadToEnd() |> pkgchk.Sca.parse

        match r with
        | Choice1Of2 xs ->
            match xs with
            | [] -> failwith "Empty list returned"
            | [ x; y ] ->
                x.framework |> should equal "net7.0"
                x.packageId |> should equal "System.Net.Http"
                x.resolvedVersion |> should equal "4.3.0"
                x.severity |> should equal "Critical"
                x.advisoryUri |> should not' (be NullOrEmptyString)

                y.framework |> should equal "net7.0"
                y.packageId |> should equal "System.Text.RegularExpressions"
                y.resolvedVersion |> should equal "4.3.1"
                y.severity |> should equal "High"
                y.advisoryUri |> should not' (be NullOrEmptyString)
            | _ -> failwith "Unrecognised list returned"
        | _ -> failwith "No error raised"

    [<Property>]
    let ``hitsByLevels on empty returns empty`` () =
        let hits = []
        let result = hits |> pkgchk.Sca.hitsByLevels []

        result |> Seq.isEmpty

    [<Property(Verbose = true)>]
    let ``hitsByLevels by vulnerable on given severity returns hits`` (severities: Guid list) =
        let severities =
            severities |> Seq.map (fun g -> g.ToString()) |> Seq.distinct |> List.ofSeq

        let hits =
            severities
            |> Seq.map (fun s ->
                { pkgchk.ScaHit.empty with
                    kind = pkgchk.ScaHitKind.Vulnerability
                    severity = s })
            |> List.ofSeq

        let knownSeverities =
            match severities |> Seq.tryHead with
            | Some s -> s
            | _ -> Guid.NewGuid().ToString() // if nothing there, we'll assume "empty"

        let result = hits |> pkgchk.Sca.hitsByLevels [ knownSeverities ]

        let expected =
            hits |> Seq.filter (fun h -> h.severity = knownSeverities) |> List.ofSeq

        expected = result

    [<Property(Verbose = true)>]
    let ``hitsByLevels by deprecated on given reason returns hits`` (reasons: Guid list) =
        let reasons =
            reasons |> Seq.map (fun g -> g.ToString()) |> Seq.distinct |> List.ofSeq

        let hits =
            reasons
            |> Seq.map (fun s ->
                { pkgchk.ScaHit.empty with
                    kind = pkgchk.ScaHitKind.Deprecated
                    reasons = [| s |] })
            |> List.ofSeq

        let knownReasons =
            match reasons |> Seq.tryHead with
            | Some s -> s
            | _ -> Guid.NewGuid().ToString() // if nothing there, we'll assume "empty"

        let result = hits |> pkgchk.Sca.hitsByLevels [ knownReasons ]

        let expected =
            hits
            |> Seq.filter (fun h -> h.reasons |> Seq.contains knownReasons)
            |> List.ofSeq

        expected = result

    [<Property(Verbose = true)>]
    let ``hitsByLevels by deprecated on given reason returns trimmed hits`` (reasons: Guid list) =
        let reasons =
            reasons |> Seq.map (fun g -> g.ToString()) |> Seq.distinct |> Array.ofSeq

        let hits =
            reasons
            |> Seq.map (fun s ->
                { pkgchk.ScaHit.empty with
                    kind = pkgchk.ScaHitKind.Deprecated
                    reasons = reasons })
            |> List.ofSeq

        let knownReasons =
            match reasons |> Seq.tryHead with
            | Some s -> s
            | _ -> Guid.NewGuid().ToString() // if nothing there, we'll assume "empty"

        let result = hits |> pkgchk.Sca.hitsByLevels [ knownReasons ]

        let expected =
            hits |> Seq.map (fun h -> { h with reasons = [| knownReasons |] }) |> List.ofSeq

        expected = result
