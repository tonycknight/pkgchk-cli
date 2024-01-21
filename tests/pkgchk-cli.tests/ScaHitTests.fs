namespace pkgchk.tests

open FsUnit.Xunit
open Xunit

module ScaHitTests =

    [<Fact>]
    let ``ScaHit.empty returns empty`` () =
        let result = pkgchk.ScaHit.empty

        result.projectPath |> should be EmptyString
        result.reasons |> should haveLength 0
        result.severity |> should be EmptyString
        result.advisoryUri |> should be EmptyString
        result.kind |> should equal pkgchk.ScaHitKind.Vulnerability
        result.packageId |> should be EmptyString
        result.resolvedVersion |> should be EmptyString
        result.alternativePackageId |> should be EmptyString
        result.suggestedReplacement |> should be EmptyString
        result.framework |> should be EmptyString
