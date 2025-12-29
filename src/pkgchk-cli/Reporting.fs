namespace pkgchk

type ReportFunc = (ApplicationContext * ApplicationScanResults * string) -> string seq

type ReportGenerationContext =
    { app: ApplicationContext
      results: ApplicationScanResults
      reportName: string
      imageUri: string
      genMarkdown: ReportFunc
      genJson: ReportFunc }

module ReportGeneration =
    
    let jsonReport (context: ApplicationContext, results: ApplicationScanResults, image: string) =
        seq { Json.serialise results.hits }

    let reports (context: ReportGenerationContext) =
        let required fmt =
            context.app.report.formats |> Seq.contains fmt

        let write name =
            let directory = Io.composeFilePath context.app.report.reportDirectory
            Io.writeFile (name |> directory)

        let reports =
            [ if required ReportFormat.Markdown then
                  (context.genMarkdown, "md")

              if required ReportFormat.Json then
                  (context.genJson, "json") ]

        reports
        |> List.map (fun (gen, ext) ->
            (context.app, context.results, context.imageUri)
            |> gen
            |> write $"{context.reportName}.{ext}")
