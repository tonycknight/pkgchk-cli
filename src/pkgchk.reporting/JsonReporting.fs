namespace pkgchk.reporting

open pkgchk

module JsonReporting =

    let generate (context: ReportGeneratorOptions) (value: 'a) =
        value |> Json.serialise |> Task.ofResult

    let build (context: ReportGeneratorOptions) (value: string) =
        task {
            let reportName = $"{context.name}.json"

            let! path =
                context.reportDirectory
                |> Io.fullPath
                |> Io.join reportName
                |> Io.writeLinesAsync [ value ]

            return ReportGenerationResult.OutputFile path
        }
