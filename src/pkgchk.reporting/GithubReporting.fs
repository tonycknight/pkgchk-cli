namespace pkgchk.reporting

open pkgchk

module GithubReporting =

    let generateComment (context: ReportGeneratorOptions) (value: 'a) =
        task {
            // build a markdown representation as a seq of strings
            // note that the size must not exceed a Github-imposed limit of let maxCommentSize = 65536
            return []
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

    // TODO:
    let build (context: ReportGeneratorOptions) (values: string seq) =
        values |> MarkdownReporting.build context
