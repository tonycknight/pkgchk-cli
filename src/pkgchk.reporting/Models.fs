namespace pkgchk.reporting

open System.Threading.Tasks

type ReportGeneratorOptions =
    { reportDirectory: string
      reportName: string }

type ReportGenerationResult = { outPath: string }

type ReportGeneratorFunc<'a, 'b> = ReportGeneratorOptions -> 'a -> Task<'b>
type ReportBuilderFunc<'a> = ReportGeneratorOptions -> 'a -> Task<ReportGenerationResult>

type ReportGeneration<'a, 'b> =
    { generate: ReportGeneratorFunc<'a, 'b>
      build: ReportBuilderFunc<'b>
      options: ReportGeneratorOptions
      data: 'a }

    static member gen (context: ReportGeneration<'a, 'b>) =
        task {            
            let! repData = context.data |> context.generate context.options

            return! repData |> context.build context.options
        }

type Table =
    { border: bool
      showHeaders: bool
      columns: string list
      rows: string list list }

    static member empty =
        { Table.columns = []
          border = false
          showHeaders = false
          rows = [] }

    static member singleRow row =
        { Table.columns = [ "" ]
          border = false
          showHeaders = false
          rows = [ [ row ] ] }

    static member addColumn name table =
        { table with
            columns = name :: table.columns }

    static member addRow row table = { table with rows = row :: table.rows }
