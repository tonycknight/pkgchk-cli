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

    [<Property(Arbitrary = [| typeof<AlphaNumericString> |], Verbose = true)>]
    let ``hitsByLevels by unknown severities returns empty`` (hits: pkgchk.ScaHit list) =
        hits |> pkgchk.Sca.hitsByLevels [] |> Seq.isEmpty

    [<Property(Arbitrary = [| typeof<AlphaNumericString>; typeof<VulnerableScaHitKind> |], Verbose = true)>]
    let ``hitsByLevels vulnerable by known severities returns hits`` (hits: pkgchk.ScaHit list) =
        if hits |> List.isEmpty then
            true
        else
            let severities = hits |> List.map _.severity |> List.distinct
            let severity = severities |> List.head
            let sort (h: pkgchk.ScaHit) = (h.kind, h.severity)

            let result = hits |> pkgchk.Sca.hitsByLevels [ severity ] |> List.sortBy sort

            let expectedHits =
                hits
                |> List.filter (fun h -> StringComparer.InvariantCultureIgnoreCase.Equals(h.severity, severity))
                |> List.sortBy sort

            result = expectedHits

    [<Property(Arbitrary =
                   [| typeof<AlphaNumericString>
                      typeof<AlphaNumericStringArray>
                      typeof<DeprecatedScaHitKind> |],
               Verbose = true)>]
    let ``hitsByLevels deprecated by known reasons returns hits`` (hits: pkgchk.ScaHit list) =
        if hits |> List.isEmpty then
            true
        else
            let reasons = hits |> Seq.collect _.reasons |> Seq.distinct |> List.ofSeq
            let reason = reasons |> List.head

            let sort (h: pkgchk.ScaHit) = (h.kind, h.reasons |> Array.head)

            let result = hits |> pkgchk.Sca.hitsByLevels [ reason ] |> List.sortBy sort

            let expectedHits =
                hits
                |> List.filter (fun h ->
                    StringComparer.InvariantCultureIgnoreCase.Equals(h.reasons |> Array.head, reason))
                |> List.sortBy sort

            result = expectedHits

    [<Property(Arbitrary = [| typeof<AlphaNumericString>; typeof<VulnerableScaHitKind> |], Verbose = true)>]
    let ``hitCountSummary on vulnerable produces counts`` (hits: pkgchk.ScaHit list) =
        let hits = hits |> List.map (fun h -> { h with reasons = [||] })
        let sort (h: pkgchk.ScaHitSummary) = (h.kind, h.severity)
        let results = pkgchk.Sca.hitCountSummary hits |> Seq.sortBy sort |> Array.ofSeq

        let groupedHits = hits |> Seq.groupBy (fun h -> (h.kind, h.severity))

        let expected =
            groupedHits
            |> Seq.map (fun ((k, s), xs) ->
                { pkgchk.ScaHitSummary.kind = k
                  pkgchk.ScaHitSummary.severity = s
                  pkgchk.ScaHitSummary.count = Seq.length xs })
            |> Seq.sortBy sort
            |> Array.ofSeq

        results = expected

    [<Property(Arbitrary =
                   [| typeof<AlphaNumericString>
                      typeof<AlphaNumericStringArray>
                      typeof<DeprecatedScaHitKind> |],
               Verbose = true)>]
    let ``hitCountSummary on deprecated produces counts`` (hits: pkgchk.ScaHit list) =
        let hits =
            hits
            |> List.map (fun h ->
                { h with
                    severity = ""
                    reasons = [| h.reasons.[0] |] })

        let sort (h: pkgchk.ScaHitSummary) = (h.kind, h.severity)
        let results = pkgchk.Sca.hitCountSummary hits |> Seq.sortBy sort |> Array.ofSeq

        let groupedHits = hits |> Seq.groupBy (fun h -> (h.kind, h.reasons |> Seq.head))

        let expected =
            groupedHits
            |> Seq.map (fun ((k, s), xs) ->
                { pkgchk.ScaHitSummary.kind = k
                  pkgchk.ScaHitSummary.severity = s
                  pkgchk.ScaHitSummary.count = Seq.length xs })
            |> Seq.sortBy sort
            |> Array.ofSeq

        results = expected
