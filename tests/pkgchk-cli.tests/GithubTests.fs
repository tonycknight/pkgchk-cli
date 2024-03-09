namespace pkgchk.tests

open System
open FsUnit.Xunit
open FsCheck.Xunit
open NSubstitute
open Octokit
open Xunit

type GithubComment = pkgchk.GithubComment

module GithubTests =

    let repo = ("testOwner", "testrepo")

    let comment text =
        new IssueComment(
            42,
            "",
            "",
            "",
            text,
            DateTimeOffset.UtcNow,
            DateTimeOffset.MinValue,
            null,
            null,
            AuthorAssociation.Collaborator
        )

    let client () = Substitute.For<IGitHubClient>()
    let issueClient () = Substitute.For<IIssuesClient>()

    let issueGet (issue: Issue) (issueClient: IIssuesClient) =
        issueClient
            .Get(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>())
            .Returns(issue)
        |> ignore

        issueClient

    let bindIssues (issueClient: IIssuesClient) (client: IGitHubClient) =
        client.Issue.Returns(issueClient) |> ignore
        client

    let commentClient () = Substitute.For<IIssueCommentsClient>()

    let commentsGet (comments: IssueComment[]) (client: IIssueCommentsClient) =
        client
            .GetAllForIssue(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>())
            .Returns(comments)
        |> ignore

        client

    let bindComments (comments: IIssueCommentsClient) (issueClient: IIssuesClient) =
        issueClient.Comment.Returns(comments) |> ignore
        issueClient

    let throwIssueException (ci: Core.CallInfo) : Octokit.Issue = failwith "boom"

    [<Fact>]
    let ``getIssueComments on no issue returns empty comments`` () =

        let commentClient = commentClient () |> commentsGet [||]
        let issueClient = issueClient () |> bindComments commentClient
        let client = client () |> bindIssues issueClient

        issueClient
            .Get(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>())
            .Returns(throwIssueException)
        |> ignore

        let rt = pkgchk.Github.getIssueComments client repo 1
        let r = rt.Result

        r |> should be Empty


    [<Fact>]
    let ``getIssueComments on empty issue returns empty comments`` () =
        let issue = new Octokit.Issue()

        let commentClient = commentClient () |> commentsGet [||]
        let issueClient = issueClient () |> issueGet issue |> bindComments commentClient
        let client = client () |> bindIssues issueClient

        let rt = pkgchk.Github.getIssueComments client repo 1
        let r = rt.Result

        r |> should be Empty

    [<Fact>]
    let ``getIssueComments on issue returns comments`` () =
        let issue = new Octokit.Issue()
        let comment = comment "just a test"
        let comments = [| comment |]

        let commentClient = commentClient () |> commentsGet comments
        let issueClient = issueClient () |> issueGet issue |> bindComments commentClient
        let client = client () |> bindIssues issueClient

        let rt = pkgchk.Github.getIssueComments client repo 1
        let r = rt.Result

        r |> should equal (List.ofSeq comments)

    [<Fact>]
    let ``setPrComment new comment invokes create`` () =
        let commentClient = commentClient ()
        let issueClient = issueClient () |> bindComments commentClient
        let client = client () |> bindIssues issueClient

        let gc =
            { GithubComment.title = "title"
              GithubComment.body = "body" }

        let pr = 1
        let rt = pkgchk.Github.setPrComment ignore client repo pr gc
        let r = rt.Result

        commentClient.Received(1).Create(fst repo, snd repo, pr, Arg.Any<string>())
        |> ignore

    [<Fact>]
    let ``setPrComment exosting comment invokes update`` () =
        let gc =
            { GithubComment.title = "title"
              GithubComment.body = "body" }

        let pr = 1
        let comment = comment $"# {gc.title}"
        let comments = [| comment |]

        let commentClient = commentClient () |> commentsGet comments
        let issueClient = issueClient () |> bindComments commentClient
        let client = client () |> bindIssues issueClient

        let rt = pkgchk.Github.setPrComment ignore client repo pr gc
        let r = rt.Result

        commentClient
            .Received(1)
            .Update(fst repo, snd repo, comment.Id, Arg.Any<string>())
        |> ignore

    [<Property(Arbitrary = [| typeof<AlphaNumericString> |], Verbose = true)>]
    let ``repo constructs owner/repo`` (name: string[]) =
        let input = name |> pkgchk.String.join "/"

        let expected =
            if name.Length = 2 then
                (name.[0], name.[1])
            else
                ("", input)

        input |> pkgchk.Github.repo = expected
