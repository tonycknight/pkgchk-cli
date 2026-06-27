namespace pkgchk.reporting

open pkgchk
open pkgchk.reporting.Console
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
            return ReportGenerationResult.Null
        }

    let renderReportFiles (results) =
        let render reportFiles =
            let msg =
                reportFiles
                |> Seq.map (fun f -> $"[link={f}]{f}[/]" |> cyan)
                |> String.joinPretty ", " " & "

            if msg.Length > 0 then
                $"{System.Environment.NewLine}Report file(s) {msg} built." |> italic |> console

        results
        |> Seq.map (function
            | OutputFile path -> path
            | _ -> "")
        |> Seq.filter (fun s -> s <> "")
        |> render

        results
