namespace pkgchk.tests

open FsCheck.Xunit

module GithubCommentTests =

    [<Property(Arbitrary = [| typeof<AlphaNumericString> |], Verbose = true)>]
    let ``create produces comment`` (title: string, body: string) =
        let r = pkgchk.GithubComment.create title body

        r.title = title && r.body = body
