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

    [<Property(Arbitrary = [| typeof<AlphaNumericString> |], Verbose = true)>]
    let ``constructComment from title and body`` (title: string, body: string) =
        let comment = GithubComment.create title body
        let (t, b) = pkgchk.Github.constructComment comment

        t.Contains(title) && b.Contains(title) && b.Contains(body)

    [<Fact>]
    let ``getIssueComments on no issue returns empty comments`` () =
        task {
            let commentClient = commentClient () |> commentsGet [||]
            let issueClient = issueClient () |> bindComments commentClient
            let client = client () |> bindIssues issueClient

            issueClient.Get(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int64>()).Returns(throwIssueException)
            |> ignore

            let! r = pkgchk.Github.getIssueComments client repo 1

            r |> should be Empty
        }

    [<Fact>]
    let ``getIssueComments on empty issue returns empty comments`` () =
        task {
            let issue = new Octokit.Issue()

            let commentClient = commentClient () |> commentsGet [||]
            let issueClient = issueClient () |> issueGet issue |> bindComments commentClient
            let client = client () |> bindIssues issueClient

            let! r = pkgchk.Github.getIssueComments client repo 1

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

            let! r = pkgchk.Github.getIssueComments client repo 1

            r |> should equal (List.ofSeq comments)
        }

    [<Property(Arbitrary = [| typeof<AlphaNumericString> |], Verbose = true)>]
    let ``setPrComment new comment invokes create`` (title: string, body: string, prId: int) =
        task {
            let commentClient = commentClient ()
            let issueClient = issueClient () |> bindComments commentClient
            let client = client () |> bindIssues issueClient

            let gc = GithubComment.create title body

            let! r = pkgchk.Github.setPrComment ignore client repo prId gc

            commentClient
                .Received(1)
                .Create(fst repo, snd repo, prId, Arg.Is<string>(fun s -> s = $"# {title}{Environment.NewLine}{body}"))
            |> ignore

            return true
        }

    [<Property(Arbitrary = [| typeof<AlphaNumericString> |], Verbose = true)>]
    let ``setPrComment existing comment invokes update`` (title: string, body: string) =
        task {
            let gc = GithubComment.create title body

            let pr = 1
            let comment = comment $"# {gc.title}"
            let comments = [| comment |]

            let commentClient = commentClient () |> commentsGet comments
            let issueClient = issueClient () |> bindComments commentClient
            let client = client () |> bindIssues issueClient

            let! r = pkgchk.Github.setPrComment ignore client repo pr gc

            commentClient
                .Received(1)
                .Update(
                    fst repo,
                    snd repo,
                    comment.Id,
                    Arg.Is<string>(fun s -> s = $"# {title}{Environment.NewLine}{body}")
                )
            |> ignore

            return true
        }

    [<Property(Arbitrary = [| typeof<AlphaNumericString> |], Verbose = true)>]
    let ``createCheck creates and sends a check``
        (owner: string, repo: string, title: string, body: string, isSuccess: bool)
        =
        task {
            let run = new CheckRun()

            let checkRunsClient = checkRunsClient ()

            checkRunsClient
                .Create(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<NewCheckRun>())
                .Returns(System.Threading.Tasks.Task.FromResult(run))
            |> ignore

            let checksClient = checksClient ()
            checksClient.Run.Returns(checkRunsClient) |> ignore
            let github = client ()
            github.Check.Returns(checksClient) |> ignore

            let comment = GithubComment.create title body

            do! pkgchk.Github.createCheck ignore github (owner, repo) "commit" isSuccess comment

            checkRunsClient
                .Received(1)
                .Create(
                    Arg.Is(owner),
                    Arg.Is(repo),
                    Arg.Is<NewCheckRun>(fun (x: NewCheckRun) ->
                        x.Output.Title = comment.title
                        && x.Output.Summary = comment.body
                        && x.Status.Value.Value = CheckStatus.Completed
                        && x.Conclusion.Value.Value = (match isSuccess with
                                                       | true -> CheckConclusion.Success
                                                       | false -> CheckConclusion.Failure))
                )
            |> ignore

            return true
        }
