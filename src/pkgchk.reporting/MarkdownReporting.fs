namespace pkgchk.reporting

open pkgchk

module MarkdownReporting =

    let generate (context: ReportGeneratorContext) (value: 'a) =
        task {
            // TODO: need to build a markdown representation as a seq of strings
            return []
        }

    let build (context: ReportGeneratorContext) (values: string seq) =
        task {
            let reportName = $"{context.reportName}.md"

            let! path =
                context.reportDirectory
                |> Io.fullPath
                |> Io.join reportName
                |> Io.writeLinesAsync values

            return { ReportGenerationResult.outPath = path }
        }
