namespace pkgchk.reporting

open System.Threading.Tasks

type ReportGeneratorOptions =
    { reportDirectory: string
      name: string
      console: Spectre.Console.IAnsiConsole
      trace: string -> unit }

    static member empty =
        { ReportGeneratorOptions.reportDirectory = ""
          name = ""
          trace = ignore
          console = Spectre.Console.AnsiConsole.Console }

type RenderKind =
    | JsonFile
    | MarkdownFile
    | GithubActionPrComment
    | GithubActionCheck
    | ConsoleRender

type ReportGenerationResult =
    | Null
    | OutputFile of path: string
    | GithubComment of comment: pkgchk.GithubComment

type ReportGeneratorFunc<'a, 'b> = ReportGeneratorOptions -> 'a -> Task<'b>
type ReportBuilderFunc<'a> = ReportGeneratorOptions -> 'a -> Task<ReportGenerationResult>

type ReportGeneration<'a, 'b> =
    { generate: ReportGeneratorFunc<'a, 'b>
      build: ReportBuilderFunc<'b>
      options: ReportGeneratorOptions
      data: 'a }

    static member gen(context: ReportGeneration<'a, 'b>) =
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
