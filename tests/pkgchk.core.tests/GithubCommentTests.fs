namespace pkgchk.core.tests

open System
open FsCheck.Xunit
open pkgchk

module GithubCommentTests =
    [<Property(Arbitrary = [| typeof<AlphaNumericString> |], Verbose = true)>]
    let ``create produces comment`` (title: string, body: string) =
        let r = GithubComment.create title body

        r.title = title && r.body = body

    [<Property(Arbitrary = [| typeof<AlphaNumericString> |], Verbose = true)>]
    let ``deconstruct produces comment title`` (title: string, body: string) =
        let (t, b) = GithubComment.create title body |> GithubComment.deconstruct

        t = $"# {title}"

    [<Property(Arbitrary = [| typeof<AlphaNumericString> |], Verbose = true)>]
    let ``deconstruct produces comment body`` (title: string, body: string) =
        let (t, b) = GithubComment.create title body |> GithubComment.deconstruct

        let expTitle = $"# {title}"

        $"{expTitle}{Environment.NewLine}{body}" = b
