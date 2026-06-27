namespace pkgchk.reporting

open pkgchk
open pkgchk.Github

module GithubReporting =

    let generateComment (context: ReportGeneratorOptions) (value: 'a) =
        task {
            // build a markdown representation as a seq of strings
            // note that the size must not exceed a Github-imposed limit of let maxCommentSize = 65536
            return value
        }

    let generateCheck (context: ReportGeneratorOptions) (value: 'a) =
        task {
            // TODO: need to build a markdown representation as a seq of strings
            return []
        }

    let generatePrComment (context: ReportGeneratorOptions) (value: 'a) =
        task {
            // TODO: need to build a markdown representation as a seq of strings
            return []
        }

    let buildComment (context: ReportGeneratorOptions) (body: string seq) =
        let body = body |> String.joinLines
        let body = 
            if body.Length < Github.maxCommentSize then body
            else "_The report is too big for Github - Please check logs_"
                    
        GithubComment.create context.name body
        |> ReportGenerationResult.GithubComment
        |> Task.ofResult