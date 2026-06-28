namespace pkgchk.core.tests

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

    let checkRunsClient () = Substitute.For<ICheckRunsClient>()
    let checksClient () = Substitute.For<IChecksClient>()
    let client () = Substitute.For<IGitHubClient>()

    let issueClient () = Substitute.For<IIssuesClient>()

    let issueGet (issue: Issue) (issueClient: IIssuesClient) =
        issueClient.Get(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int64>()).Returns(issue)
        |> ignore

        issueClient

    let bindIssues (issueClient: IIssuesClient) (client: IGitHubClient) =
        client.Issue.Returns(issueClient) |> ignore
        client

    let commentClient () = Substitute.For<IIssueCommentsClient>()

    let commentsGet (comments: IssueComment[]) (client: IIssueCommentsClient) =
        client.GetAllForIssue(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int64>()).Returns(comments)
        |> ignore

        client

    let bindComments (comments: IIssueCommentsClient) (issueClient: IIssuesClient) =
        issueClient.Comment.Returns(comments) |> ignore
        issueClient

    let throwIssueException (ci: Core.CallInfo) : Octokit.Issue = failwith "boom"

    [<Fact>]
    let ``getIssueComments on no issue returns empty comments`` () =
        task {
            let commentClient = commentClient () |> commentsGet [||]
            let issueClient = issueClient () |> bindComments commentClient
            let client = client () |> bindIssues issueClient

            issueClient.Get(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int64>()).Returns(throwIssueException)
            |> ignore

            let! r = pkgchk.Github.getIssueComments client ignore repo 1

            r |> should be Empty
        }

    [<Fact>]
    let ``getIssueComments on empty issue returns empty comments`` () =
        task {
            let issue = new Octokit.Issue()

            let commentClient = commentClient () |> commentsGet [||]
            let issueClient = issueClient () |> issueGet issue |> bindComments commentClient
            let client = client () |> bindIssues issueClient

            let! r = pkgchk.Github.getIssueComments client ignore repo 1

            r |> should be Empty
        }

    [<Fact>]
    let ``getIssueComments on issue returns comments`` () =
        task {
            let issue = new Octokit.Issue()
            let comment = comment "just a test"
            let comments = [| comment |]

            let commentClient = commentClient () |> commentsGet comments
            let issueClient = issueClient () |> issueGet issue |> bindComments commentClient
            let client = client () |> bindIssues issueClient

            let! r = pkgchk.Github.getIssueComments client ignore repo 1

            r |> should equal (List.ofSeq comments)
        }
