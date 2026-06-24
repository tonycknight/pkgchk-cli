namespace pkgchk.reporting

open pkgchk

module JsonReporting =
    
    let generate (context: ReportGeneratorContext) (value: 'a) =
        value |> Json.serialise |> Task.ofResult

    let build (context: ReportGeneratorContext) (value: string) =
        task {
            let reportName = $"{context.reportName}.json"

            let! path = 
                context.reportDirectory 
                |> Io.fullPath 
                |> Io.join reportName
                |> Io.writeLinesAsync [value]
            
            return { ReportGenerationResult.outPath = path }
        }
        