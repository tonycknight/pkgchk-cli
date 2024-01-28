namespace pkgchk.tests

open System
open FsCheck.Xunit

module GithubRepoTests =

    [<Property(Arbitrary = [| typeof<AlphaNumericString> |], Verbose = true)>]
    let ``repo constructs owner/repo`` (name: string[]) =
        let input = name |> pkgchk.String.join "/"

        let expected =
            if name.Length = 2 then
                (name.[0], name.[1])
            else
                ("", input)

        input |> pkgchk.GithubRepo.repo = expected
