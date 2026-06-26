namespace pkgchk.reporting

open pkgchk

module MarkdownReporting =

    let generate (context: ReportGeneratorOptions) (value: 'a) =
        task {
            // TODO: need to build a markdown representation as a seq of strings
            return [ "# Report generated here"]
        }

    let build (context: ReportGeneratorOptions) (values: string seq) =
        task {
            let reportName = $"{context.reportName}.md"

            let! path =
                context.reportDirectory
                |> Io.fullPath
                |> Io.join reportName
                |> Io.writeLinesAsync values

            return { ReportGenerationResult.outPath = path }
        }
