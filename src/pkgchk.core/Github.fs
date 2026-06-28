namespace pkgchk

open System
open System.Diagnostics.CodeAnalysis
open pkgchk
open Octokit

type GithubRepo =
    { owner: string
      repo: string }

    static member create name =
        let (name, repo) = String.split '/' name
        { GithubRepo.owner = name; repo = repo }

type GithubComment =
    { title: string
      body: string }

    static member create title body =
        { GithubComment.title = (String.defaultValue "pkgchk summary" title)
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

    let getIssue (client: IGitHubClient) (owner: string, repo: string) id =
        task {
            try
                let! issue = client.Issue.Get(owner, repo, id)
                return Some issue
            with ex ->
                return None
        }

    let getIssueComments (client: IGitHubClient) trace (owner: string, repo) id =
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
