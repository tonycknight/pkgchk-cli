namespace pkgchk.tests

open System
open FsCheck.Xunit

module ScaModelsTests =

    [<Property(MaxTest = 1)>]
    let ``hitsByLevels on empty returns empty`` () =
        let hits = []
        let result = hits |> pkgchk.ScaModels.hitsByLevels []

        result |> Seq.isEmpty

    [<Property(Arbitrary = [| typeof<AlphaNumericString> |], Verbose = true)>]
    let ``hitsByLevels by unknown severities returns empty`` (hits: pkgchk.ScaHit list) =
        hits |> pkgchk.ScaModels.hitsByLevels [] |> Seq.isEmpty

    [<Property(Arbitrary = [| typeof<AlphaNumericString>; typeof<VulnerableScaHitKind> |], Verbose = true)>]
    let ``hitsByLevels vulnerable by known severities returns hits`` (hits: pkgchk.ScaHit list) =
        if hits |> List.isEmpty then
            true
        else
            let severities = hits |> List.map _.severity |> List.distinct
            let severity = severities |> List.head
            let sort (h: pkgchk.ScaHit) = (h.kind, h.severity)

            let result = hits |> pkgchk.ScaModels.hitsByLevels [ severity ] |> List.sortBy sort

            let expectedHits =
                hits
                |> List.filter (fun h -> StringComparer.InvariantCultureIgnoreCase.Equals(h.severity, severity))
                |> List.sortBy sort

            result = expectedHits

    [<Property(Arbitrary =
                   [| typeof<AlphaNumericString>
                      typeof<AlphaNumericStringSingletonArray>
                      typeof<DeprecatedScaHitKind> |],
               Verbose = true)>]
    let ``hitsByLevels deprecated by known reasons returns hits`` (hits: pkgchk.ScaHit list) =
        if hits |> List.isEmpty then
            true
        else
            let reasons = hits |> Seq.collect _.reasons |> Seq.distinct |> List.ofSeq
            let reason = reasons |> List.head

            let sort (h: pkgchk.ScaHit) = (h.kind, h.reasons |> Array.head)

            let result = hits |> pkgchk.ScaModels.hitsByLevels [ reason ] |> List.sortBy sort

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

        let results =
            pkgchk.ScaModels.hitCountSummary hits |> Seq.sortBy sort |> Array.ofSeq

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
                      typeof<AlphaNumericStringSingletonArray>
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

        let results =
            pkgchk.ScaModels.hitCountSummary hits |> Seq.sortBy sort |> Array.ofSeq

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
