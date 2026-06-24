namespace pkgchk.reporting

open System.Threading.Tasks

type ReportGeneratorContext =
    { reportDirectory: string
      reportName: string
      imageUri: string }


type ReportGenerationResult = { outPath: string }

type ReportGeneratorFunc<'a, 'b> = ReportGeneratorContext -> 'a -> Task<'b>
type ReportBuilderFunc<'a> = ReportGeneratorContext -> 'a -> Task<ReportGenerationResult>

type ReportGenerator<'a, 'b> =
    { generate: ReportGeneratorFunc<'a, 'b>
      build: ReportBuilderFunc<'b>
      context: ReportGeneratorContext
      data: 'a[] }
