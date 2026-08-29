namespace pkgchk.reporting

open pkgchk
open pkgchk.Github

module GithubReporting =

    let generateComment (context: ReportGeneratorOptions) (body: 'a) =
        task {
            let body = body |> String.joinLines

            let body =
                if body.Length < Github.maxCommentSize then
                    body
                else
                    "_The report is too big for Github - Please check logs_"

            return { GithubComment.create context.name body with isSuccess = context.isSuccess }
        }

    let buildCheck (context: ReportGeneratorOptions) (comment: GithubComment) =
        task {            
            let client = Github.client context.githubToken
            let repo = GithubRepo.create context.githubRepo

            context.trace $"Posting {comment.title} build check to Github repo {repo}..." // TODO: ToString() rep?

            // TODO: make a proxy...
            do! Github.createCheck context.trace client repo context.githubCommit context.isSuccess comment
            
            return ReportGenerationResult.GithubComment comment
        }

    let buildPrComment (context: ReportGeneratorOptions) (comment: GithubComment) =
        task {
            // TODO: send...
            //do! Github.sendPrComment context comment
            let client = Github.client context.githubToken
            let repo = GithubRepo.create context.githubRepo

            context.trace $"Posting {comment.title} PR comment to Github repo {repo}..."

            // TODO: 

            return ReportGenerationResult.GithubComment comment
        }
