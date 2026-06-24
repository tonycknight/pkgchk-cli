namespace pkgchk.reporting

open Spectre.Console
open Spectre.Console.Rendering

module ConsoleReporting =

    let build (console: IAnsiConsole) (context: ReportGeneratorContext) (values: IRenderable seq) =
        task {
            // TODO:
            return { ReportGenerationResult.outPath = "" }
        }
