namespace pkgchk

open System
open Octokit

type GithubComment = { title: string; body: string }

module Github =

    let client token =
        let header = new ProductHeaderValue(App.packageId)
        let client = new GitHubClient(header) 
        client.Credentials <- new Credentials(token)
        client :> IGitHubClient

    let getIssue (client: IGitHubClient) (owner: string, repo) id =
        task {
            try
                let! issue = client.Issue.Get(owner, repo, id)
                return Some issue
            with ex ->
                return None
        }

    let getIssueComments (client: IGitHubClient) (owner: string, repo) id =
        task {
            try
                let! issue = getIssue client (owner, repo) id

                let! comments =
                    match issue with
                    | Some issue ->
                        task {
                            let! x = client.Issue.Comment.GetAllForIssue(owner, repo, id)
                            return x |> List.ofSeq
                        }
                    | None -> task { return [] }

                return comments
            with ex ->
                return []
        }

    let setPrComment (client: IGitHubClient) (owner, repo) prId (comment: GithubComment) =
        task {
            let commentBody = $"{comment.title}{Environment.NewLine}{comment.body}"

            // find those comments whose body starts with comment.Title
            // edit if found; create if not
            let! comments = getIssueComments client (owner, repo) prId

            let previousComment =
                comments
                |> Seq.filter (fun c -> c.Body.StartsWith(comment.title, StringComparison.InvariantCulture))
                |> Seq.tryHead

            let! newComment =
                match previousComment with
                | Some c ->
                    task {
                        let! x = client.Issue.Comment.Update(owner, repo, c.Id, commentBody)
                        return x
                    }
                | None ->
                    task {
                        let! x = client.Issue.Comment.Create(owner, repo, prId, commentBody)
                        return x
                    }

            return newComment
        }
