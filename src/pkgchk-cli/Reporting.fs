namespace pkgchk

type ReportFunc = (ApplicationContext * ApplicationScanResults * string) -> string seq

type ReportGenerationContext =
    { app: ApplicationContext
      results: ApplicationScanResults
      imageUri: string
      genMarkdown: (string * ReportFunc)
      genJson: (string * ReportFunc) }

module ReportGeneration =
    open Newtonsoft.Json

    let private jsonSerialise =
        let settings = new JsonSerializerSettings()
        settings.Formatting <- Formatting.Indented
        settings.Converters.Add(new Converters.StringEnumConverter())

        fun value -> JsonConvert.SerializeObject(value, settings)

    let jsonReport (context: ApplicationContext, results: ApplicationScanResults, image: string) =
        seq { jsonSerialise results.hits }

    let reports (context: ReportGenerationContext) =
        let directory = Io.composeFilePath context.app.report.reportDirectory
        let writeFile name = Io.writeFile (name |> directory)

        [ if
              context.app.report.formats |> Seq.isEmpty
              || context.app.report.formats |> Seq.contains ReportFormat.Markdown
          then
              let (n, f) = context.genMarkdown
              (context.app, context.results, context.imageUri) |> f |> writeFile n

          if context.app.report.formats |> Seq.contains ReportFormat.Json then
              let (n, f) = context.genJson
              (context.app, context.results, context.imageUri) |> f |> writeFile n ]
