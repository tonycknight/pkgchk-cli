namespace pkgchk

open System
open Spectre.Console

module Console =
    let markup (style: string) (value: string) = $"[{style}]{value}[/]"
    let italic = markup "italic"
    let white = markup "white"
    let green = markup "lime"
    let cyan = markup "cyan"
    let error = markup "red"

    let table () =
        let table = (new Table()).LeftAligned()
        table.Border <- TableBorder.None
        table.ShowHeaders <- false
        table

    let tableColumn (name: string) (table: Table) = table.AddColumn(name)

    let colouriseReason value =
        let colour = Rendering.reasonColour value
        value |> markup colour

    let colouriseSeverity value =
        let code =
            $"{Rendering.severityStyle value} {Rendering.severityColour value}"
            |> String.trim

        value |> markup code

    let colouriseProject = markup "bold yellow"

    let nugetLinkPkgVsn package version =
        let url = $"{Rendering.nugetPrefix}/{package}/{version}"
        $"[link={url}]{package} {version}[/]"

    let nugetLinkPkgSuggestion package suggestion =
        let url = $"{Rendering.nugetPrefix}/{package}"
        $"[link={url}]{package} {suggestion}[/]"

    let title hits =
        match hits with
        | [] -> seq { green "No vulnerabilities found!" }
        | _ -> seq { error "Vulnerabilities found!" }

    let formatSeverities severities =
        severities
        |> Seq.map colouriseSeverity
        |> List.ofSeq
        |> String.joinPretty ", " " or "
        |> sprintf "Vulnerabilities found matching %s"
        |> italic
        |> Seq.singleton

    let projectTable (project: string) =
        let table = table () |> tableColumn ""
        $"Project {project}" |> colouriseProject |> Array.singleton |> table.AddRow

    let hitPackage (hit: ScaHit) =
        match hit.kind with
        | ScaHitKind.VulnerabilityTransitive
        | ScaHitKind.Vulnerability -> nugetLinkPkgVsn hit.packageId hit.resolvedVersion |> cyan
        | ScaHitKind.Deprecated -> nugetLinkPkgVsn hit.packageId hit.resolvedVersion |> cyan
        |> Seq.singleton

    let hitAdvisory hit =
        seq {
            if String.isNotEmpty hit.advisoryUri then
                yield (italic hit.advisoryUri)
        }

    let hitSeverities (hit: ScaHit) =
        seq {
            if hit.severity |> String.isNotEmpty then
                yield colouriseSeverity hit.severity

            yield! hit.reasons |> Seq.map colouriseReason
        }
        |> Seq.filter String.isNotEmpty
        |> String.joinLines

    let hitReasons hit =
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

    let hitDetails (hit: ScaHit) =
        seq {
            hitPackage hit
            hitAdvisory hit
            hitReasons hit
        }
        |> Seq.collect id
        |> Seq.filter String.isNotEmpty
        |> String.joinLines

    let hitRow (hit: ScaHit) =
        [| Rendering.formatHitKind hit.kind; hitSeverities hit; hitDetails hit |]

    let hitGroupTable (hits: seq<ScaHit>) =
        let table =
            table ()
            |> tableColumn "Kind"
            |> tableColumn "Severity"
            |> tableColumn "Resolution"

        table.Columns[0].Width <- Rendering.maxHitKindLength ()

        let rows = hits |> Seq.map hitRow

        rows |> Seq.iter (fun r -> table.AddRow r |> ignore)

        table


    let hitsTable (hits: seq<ScaHit>) =
        let table = table () |> tableColumn ""

        let innerTables =
            hits
            |> Seq.groupBy _.projectPath
            |> Seq.sortBy fst
            |> Seq.collect (fun (project, hits) ->
                seq {
                    project |> projectTable :> Spectre.Console.Rendering.IRenderable
                    hits |> hitGroupTable
                    new Text("")
                })

        innerTables |> Seq.iter (fun tr -> table.AddRow tr |> ignore)

        table

    let headlineTable errorHits =
        let table = table () |> tableColumn ""

        let title = errorHits |> title |> Array.ofSeq

        table.AddRow title


    let severitySettingsTable severities =
        let table = table () |> tableColumn ""

        let row = formatSeverities severities |> Array.ofSeq

        table.AddRow row

    let hitSummaryRow (value: ScaHitSummary) =
        let fmtSeverity kind severity =
            match kind with
            | ScaHitKind.VulnerabilityTransitive
            | ScaHitKind.Vulnerability -> colouriseSeverity severity
            | ScaHitKind.Deprecated -> colouriseReason severity

        let fmtCount value =
            match value with
            | 1 -> $"{value} hit"
            | _ -> $"{value} hits"

        [| Rendering.formatHitKind value.kind
           fmtSeverity value.kind value.severity
           fmtCount value.count |]

    let hitSummaryTable (counts: seq<ScaHitSummary>) =
        let table =
            table () |> tableColumn "Kind" |> tableColumn "Severity" |> tableColumn "Counts"

        let rows = counts |> Seq.map hitSummaryRow

        rows |> Seq.iter (table.AddRow >> ignore)

        table
