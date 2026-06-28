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
            
            return GithubComment.create context.name body
        }

    let buildCheck (context: ReportGeneratorOptions) (comment: GithubComment) =
        task {
            // TODO: send... 
            // isGoodScan is a problem... necessary to stop builds
            //do! Github.sendCheck context results.isGoodScan comment

            return ReportGenerationResult.GithubComment comment
        }
        
    let buildPrComment (context: ReportGeneratorOptions) (comment: GithubComment) =
        task {            
            // TODO: send...
            //do! Github.sendPrComment context comment

            return ReportGenerationResult.GithubComment comment
        }