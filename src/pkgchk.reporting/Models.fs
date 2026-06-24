namespace pkgchk.reporting

open System.Threading.Tasks

type ReportGeneratorOptions =
    { reportDirectory: string
      reportName: string
      imageUri: string }

type ReportGenerationResult = { outPath: string }

type ReportGeneratorFunc<'a, 'b> = ReportGeneratorOptions -> 'a -> Task<'b>
type ReportBuilderFunc<'a> = ReportGeneratorOptions -> 'a -> Task<ReportGenerationResult>

type ReportGenerator<'a, 'b> =
    { generate: ReportGeneratorFunc<'a, 'b>
      build: ReportBuilderFunc<'b>
      options: ReportGeneratorOptions
      data: 'a }

    member this.Generate() =
        task {
            let! repData = this.data |> this.generate this.options

            return! repData |> this.build this.options
        }
