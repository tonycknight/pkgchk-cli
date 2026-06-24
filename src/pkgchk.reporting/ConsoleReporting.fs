namespace pkgchk.reporting

open Spectre.Console
open Spectre.Console.Rendering

module ConsoleReporting =

    let generate (context: ReportGeneratorOptions) (value: 'a) =
        task {
            // TODO: need to build a representation as a seq of IRenderables
            return []
        }

    let build (console: IAnsiConsole) (context: ReportGeneratorOptions) (values: IRenderable seq) =
        task {
            values |> Seq.iter console.Write
            return { ReportGenerationResult.outPath = "" }
        }
