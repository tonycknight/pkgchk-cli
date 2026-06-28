namespace pkgchk

open System
open pkgchk.reporting.Console
open pkgchk.Github
open Octokit

module Github =

    let constructComment (comment: GithubComment) =
        let commentTitle = $"# {comment.title}"
        let commentBody = $"{commentTitle}{Environment.NewLine}{comment.body}"

        (commentTitle, commentBody)

    let setPrComment trace (client: IGitHubClient) (owner, repo) prId (comment: GithubComment) =
        task {

            let (commentTitle, commentBody) = constructComment comment

            // As there's no concrete mechanism in Octokit to affinitise comments, we must use titles as the discriminator.
            let! comments = Github.getIssueComments client trace (owner, repo) prId

            $"Found {comments |> Seq.length} comments." |> trace

            let previousComment =
                comments
                |> Seq.filter (fun c -> c.Body.StartsWith(commentTitle, StringComparison.InvariantCulture))
                |> Seq.tryHead

            let! newComment =
                match previousComment with
                | Some c ->
                    task {
                        $"Updating Github comment {c.Id}..." |> trace
                        let! x = client.Issue.Comment.Update(owner, repo, c.Id, commentBody)
                        return x
                    }
                | None ->
                    task {
                        "Creating new Github comment..." |> trace
                        let! x = client.Issue.Comment.Create(owner, repo, prId, commentBody)
                        return x
                    }

            return newComment
        }

    let createCheck trace (client: IGitHubClient) (owner, repo) commit isSuccess (comment: GithubComment) =
        task {
            $"Creating check for commit {commit}..." |> trace

            let checkRun = new NewCheckRun(comment.title, commit)
            checkRun.Status <- CheckStatus.Completed

            checkRun.Conclusion <-
                match isSuccess with
                | true -> CheckConclusion.Success
                | _ -> CheckConclusion.Failure

            checkRun.Output <- new NewCheckRunOutput(comment.title, comment.body)

            let! run = client.Check.Run.Create(owner, repo, checkRun)

            $"Created check for commit {run.HeadSha}, url: {run.Url}." |> trace
        }

    let sendPrComment (context: ApplicationContext) (comment: GithubComment) =
        task {
            let prId = String.toInt context.github.prId
            let repo = String.split '/' context.github.repo
            let client = Github.client context.github.token

            context.services.trace $"Posting {comment.title} PR comment to Github repo {repo}..."

            let! _ = (comment |> setPrComment context.services.trace client repo prId)

            $"{comment.title} PR report sent to Github." |> italic |> CliCommands.console
        }

    let sendCheck (context: ApplicationContext) isSuccess (comment: GithubComment) =
        task {
            let trace = context.services.trace
            let client = Github.client context.github.token
            let repo = (String.split '/' context.github.repo)

            trace $"Posting {comment.title} build check to Github repo {context.github.repo}..."

            let! _ = comment |> createCheck trace client repo context.github.commit isSuccess

            $"Check '{comment.title}' sent to Github." |> italic |> CliCommands.console
        }
