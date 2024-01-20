namespace pkgchk

open System
open Spectre.Console

module Console =

    let italic value = $"[italic]{value}[/]"
    let cyan value = $"[cyan]{value}[/]"
    let error value = $"[red]{value}[/]"

    let kindIndent (kind: ScaHitKind) =
        kind |> Rendering.formatHitKind |> _.Length |> (+) 2 |> String.indent

    let maxKindIndent () =
        [ ScaHitKind.VulnerabilityTransitive
          ScaHitKind.Vulnerability
          ScaHitKind.Deprecated ]
        |> Seq.map (kindIndent >> _.Length)
        |> Seq.max

    let formatReason value =
        let colour = Rendering.reasonColour value
        $"[{colour}]{value}[/]"

    let formatReasons = Seq.map formatReason >> String.join ", "

    let formatSeverity value =
        let code =
            $"{Rendering.severityStyle value} {Rendering.severityColour value}"
            |> String.trim

        $"[{code}]{value}[/]"

    let nugetLinkPkgVsn package version =
        let url = $"{Rendering.nugetPrefix}/{package}/{version}"
        $"[link={url}]{package} {version}[/]"

    let nugetLinkPkgSuggestion package suggestion =
        let url = $"{Rendering.nugetPrefix}/{package}"
        $"[link={url}]{package} {suggestion}[/]"

    let formatProject value = $"[bold yellow]{value}[/]"

    let formatHits (hits: seq<ScaHit>) =

        let fmt (hit: ScaHit) =
            seq {
                match hit.kind with
                | ScaHitKind.VulnerabilityTransitive
                | ScaHitKind.Vulnerability ->
                    sprintf
                        "%s: %s - %s"
                        (Rendering.formatHitKind hit.kind)
                        (formatSeverity hit.severity)
                        (nugetLinkPkgVsn hit.packageId hit.resolvedVersion |> cyan)
                | ScaHitKind.Deprecated ->
                    sprintf
                        "%s: %s"
                        (Rendering.formatHitKind hit.kind)
                        (nugetLinkPkgVsn hit.packageId hit.resolvedVersion |> cyan)

                if String.isNotEmpty hit.advisoryUri then
                    sprintf "%s%s" (kindIndent hit.kind) (italic hit.advisoryUri)

                if (hit.reasons |> Array.isEmpty |> not) then
                    if String.isNotEmpty hit.suggestedReplacement then
                        sprintf
                            "%s%s - %s"
                            (kindIndent hit.kind)
                            (formatReasons hit.reasons)
                            (match (hit.suggestedReplacement, hit.alternativePackageId) with
                             | "", _ -> ""
                             | x, y when x <> "" && y <> "" -> nugetLinkPkgSuggestion y x |> cyan |> sprintf "Use %s"
                             | x, _ -> x |> cyan |> sprintf "Use %s")
                        |> italic
                    else
                        sprintf "%s%s" (kindIndent hit.kind) (formatReasons hit.reasons) |> italic

                ""
            }

        let fmtGrp (hit: (string * seq<ScaHit>)) =
            let projectPath, hits = hit

            let hits =
                hits
                |> Seq.sortBy (fun h ->
                    (match h.kind with
                     | ScaHitKind.Vulnerability -> 0
                     | _ -> 1),
                    h.packageId)

            seq {
                $"Project: {projectPath}" |> formatProject
                yield! hits |> Seq.collect fmt
            }

        hits
        |> Seq.groupBy (fun h -> h.projectPath)
        |> Seq.sortBy fst
        |> Seq.collect fmtGrp

    let title hits =
        match hits with
        | [] -> seq { "[lime]No vulnerabilities found.[/]" }
        | _ -> seq { "[red]Vulnerabilities found![/]" }

    let formatSeverities severities =
        severities
        |> Seq.map formatSeverity
        |> List.ofSeq
        |> String.joinPretty "," "or"
        |> sprintf "Vulnerabilities found matching %s"
        |> italic
        |> Seq.singleton


    let formatHitCounts counts =
        counts
        |> Seq.map (fun (k, s, c) ->
            let fmtCount value =
                match value with
                | 1 -> $"{value} hit"
                | _ -> $"{value} hits"

            let fmtSeverity =
                function
                | ScaHitKind.VulnerabilityTransitive
                | ScaHitKind.Vulnerability -> formatSeverity
                | ScaHitKind.Deprecated -> formatReason

            $"{Rendering.formatHitKind k} - {fmtSeverity k s}: {fmtCount c}.")
        |> List.ofSeq



    let reportFileBuilt path =
        $"Report file [link={path}]{path}[/] built." |> italic

    let send (console: IAnsiConsole) = console.MarkupLine
    let write (console: IAnsiConsole) = console.Write

    let tabularProject (project: string) =
        let table = (new Table()).LeftAligned().AddColumn("")
        //table.Width <- 80 // use standard builder for this???
        table.Border <- TableBorder.None
        table.ShowHeaders <- false
        // TODO: clean up
        $"Project {project}" |> formatProject |> Array.singleton |> table.AddRow //|> ignore

    //table


    let tableHitRow hit =
        match hit.kind with
        | ScaHitKind.VulnerabilityTransitive
        | ScaHitKind.Vulnerability -> nugetLinkPkgVsn hit.packageId hit.resolvedVersion |> cyan
        | ScaHitKind.Deprecated -> nugetLinkPkgVsn hit.packageId hit.resolvedVersion |> cyan
        |> Seq.singleton

    let tableHitAdvisory hit =
        seq {
            if String.isNotEmpty hit.advisoryUri then
                yield (italic hit.advisoryUri)
        }

    let tableHitSeverities hit =
        seq {
            if hit.severity |> String.isNotEmpty then
                yield formatSeverity hit.severity

            yield! hit.reasons |> Seq.map formatReason
        }
        |> Seq.filter String.isNotEmpty
        |> String.joinLines

    let tableHitReasons hit =
        seq {
            if
                (hit.reasons |> Array.isEmpty |> not)
                && String.isNotEmpty hit.suggestedReplacement
            then
                yield
                    (match (hit.suggestedReplacement, hit.alternativePackageId) with
                     | "", _ -> ""
                     | x, y when x <> "" && y <> "" -> nugetLinkPkgSuggestion y x |> cyan |> sprintf "Use %s"
                     | x, _ -> x |> cyan |> sprintf "Use %s")
                    |> italic
        }

    let tabularHitGroup (hits: seq<ScaHit>) =
        let table =
            (new Table())
                .LeftAligned()
                .AddColumn("Kind")
                .AddColumn("Severity")
                .AddColumn("Resolution")

        table.Columns[0].Width <- maxKindIndent ()
        table.ShowHeaders <- false
        table.Border <- TableBorder.None

        let rows =
            hits
            |> Seq.map (fun h ->
                [| Rendering.formatHitKind h.kind
                   tableHitSeverities h
                   seq {
                       tableHitRow h
                       tableHitAdvisory h
                       tableHitReasons h
                   }
                   |> Seq.collect id
                   |> Seq.filter String.isNotEmpty
                   |> String.joinLines |])

        rows |> Seq.iter (fun r -> table.AddRow r |> ignore)

        table


    let tabularHits (hits: seq<ScaHit>) =
        let table = (new Table()).AddColumn("")
        table.Border <- TableBorder.None

        let innerTables =
            hits
            |> Seq.groupBy _.projectPath
            |> Seq.sortBy fst
            |> Seq.collect (fun (project, hits) ->
                seq {
                    project |> tabularProject
                    hits |> tabularHitGroup
                })
            |> Seq.map (fun tr -> [| tr :> Spectre.Console.Rendering.IRenderable |])        

        innerTables        
        |> Seq.iter (fun tr -> table.AddRow tr |> ignore)

        table