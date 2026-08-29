namespace pkgchk

open System
open System.Diagnostics.CodeAnalysis
open pkgchk
open Octokit

type GithubRepo =
    { owner: string
      repo: string }

    static member create name = // TODO: tests
        let (name, repo) = String.split '/' name
        { GithubRepo.owner = name; repo = repo }

type GithubComment =
    { title: string
      body: string 
      isSuccess: bool }

    static member create title body =
        { GithubComment.title = (String.defaultValue "pkgchk summary" title)
          isSuccess = false
          body = body }

    static member deconstruct(comment: GithubComment) =
        let commentTitle = $"# {comment.title}"
        let commentBody = $"{commentTitle}{Environment.NewLine}{comment.body}"

        (commentTitle, commentBody)

module Github =

    [<Literal>]
    let maxCommentSize = 65536

    [<ExcludeFromCodeCoverage>]
    let client token =
        let header = new ProductHeaderValue(App.packageId)
        let client = new GitHubClient(header)
        client.Credentials <- new Credentials(token)
        client :> IGitHubClient
    
    // TODO: create a GithubProxy class...
    let getIssue (client: IGitHubClient) (owner: string, repo: string) id = // TODO: refactor to use GithubRepo
        task {
            try
                let! issue = client.Issue.Get(owner, repo, id)
                return Some issue
            with ex ->
                return None
        }

    let getIssueComments (client: IGitHubClient) trace (owner: string, repo) id = // TODO: refactor to use GithubRepo
        task {
            try
                trace $"Fetching comments for issue {id}..."

                let! comments = client.Issue.Comment.GetAllForIssue(owner, repo, id)
                trace $"Fetched {comments |> Seq.length} comments for id {id}."
                return comments |> List.ofSeq

            with ex ->
                trace $"Failed to fetch comments for issue {id}: {ex.Message}"
                return []
        }

    let createCheck trace (client: IGitHubClient) (repo: GithubRepo) commit isSuccess (comment: GithubComment) =
        task {
            $"Creating check for commit {commit}..." |> trace

            let checkRun = new NewCheckRun(comment.title, commit)
            checkRun.Status <- CheckStatus.Completed

            checkRun.Conclusion <-
                match isSuccess with
                | true -> CheckConclusion.Success
                | _ -> CheckConclusion.Failure

            checkRun.Output <- new NewCheckRunOutput(comment.title, comment.body)

            let! run = client.Check.Run.Create(repo.owner, repo.repo, checkRun)

            $"Created check for commit {run.HeadSha}, url: {run.Url}." |> trace
        }