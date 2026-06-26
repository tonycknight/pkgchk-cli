namespace pkgchk

open System
open System.Collections.Concurrent
open pkgchk.reporting.Console
open Spectre.Console
open Tk.Nuget

type ReportTable = pkgchk.reporting.Table

module Console =

    let table () =
        let table = new Table()
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

    let colouriseProject = markup $"bold {Colours.yellow}"

    let nugetLinkPkgVsn package version =
        let url = $"{Rendering.nugetPrefix}/{package}/{version}"
        $"[link={url}]{package} {version}[/]"

    let nugetLinkPkgVsnOnly package version =
        let url = $"{Rendering.nugetPrefix}/{package}/{version}"
        $"[link={url}]{version}[/]"

    let nugetLinkPkgSuggestion package suggestion =
        let url = $"{Rendering.nugetPrefix}/{package}"
        $"[link={url}]{package} {suggestion}[/]"

    let hitFramework =

        let last = new ConcurrentDictionary<string, (string * string)>()
        last.[""] <- ("", "")

        let switchColour value =
            match value with
            | Colours.cornflowerblue -> Colours.lightcornflowerblue
            | _ -> Colours.cornflowerblue

        fun (hit: ScaHit) ->
            let t = last.[""]
            let mutable colour = snd t

            if String.toLower hit.framework <> fst t then
                colour <- switchColour colour
                last.[""] <- (String.toLower hit.framework, colour)

            hit.framework |> markup colour

    let vulnerabilitySummaryTitle hits =
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

    let projectTable (project: string) =
        colouriseProject $"Project {project}"
        |> ReportTable.singleRow
        |> pkgchk.reporting.Console.table

    let hitPackage (hit: ScaHit) =
        match hit.kind with
        | ScaHitKind.VulnerabilityTransitive
        | ScaHitKind.Vulnerability
        | ScaHitKind.Dependency
        | ScaHitKind.DependencyTransitive ->
            $"{hitFramework hit} {nugetLinkPkgVsn hit.packageId hit.resolvedVersion |> markup Colours.lightcyan}"
        | ScaHitKind.Deprecated ->
            $"{hitFramework hit} {nugetLinkPkgVsn hit.packageId hit.resolvedVersion |> lightcyan}"
        | x -> failwith $"Unrecognised value {x}"
        |> Seq.singleton

    let hitAdvisory hit =
        seq {
            if String.isNotEmpty hit.advisoryUri then
                italic hit.advisoryUri
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


    let packageDetails (hit: ScaHit) =
        let trimNewLines (value: string) =
            value.Split([| '\n'; '\r' |], StringSplitOptions.RemoveEmptyEntries)
            |> Seq.map String.trim
            |> Seq.filter String.isNotEmpty
            |> String.joinLines

        match hit.metadata with
        | None -> []
        | Some meta ->
            [ meta.projectUrl |> Option.map green |> Option.defaultValue ""
              sprintf
                  "%s %s"
                  (match (meta.license |> Option.ofNull |> Option.defaultValue "", meta.licenseUrl) with
                   | ("", Some url) -> url |> yellow
                   | ("", None) -> "No licence given" |> yellow
                   | (l, _) -> l |> yellow)
                  (meta.authors |> darkcyan)
              |> italic
              meta.description
              |> trimNewLines
              |> String.nonEmpty
              |> Option.map (Markup.Escape >> lightgrey >> italic)
              |> Option.defaultValue ""
              meta.tags
              |> String.nonEmpty
              |> Option.map (String.trim >> Markup.Escape >> grey >> italic)
              |> Option.defaultValue "" ]
            |> List.filter String.isNotEmpty

    let hitDetails (hit: ScaHit) =
        seq {
            hitPackage hit
            hitAdvisory hit
            hitReasons hit
            packageDetails hit
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
            |> Seq.groupBy (fun x -> x.projectPath)
            |> Seq.sortBy fst
            |> Seq.collect (fun (project, hits) ->
                seq {
                    project |> projectTable |> toRenderable
                    hits |> hitGroupTable |> toRenderable
                    new Text("") |> toRenderable
                })

        innerTables |> Seq.iter (fun tr -> table.AddRow tr |> ignore)

        table

    let vulnerabilityHeadlineTable hits =
        let table = table () |> tableColumn ""

        let title = hits |> vulnerabilitySummaryTitle |> Array.ofSeq

        table.AddRow title

    let noscanHeadlineTable () =
        green "Nothing found!"
        |> ReportTable.singleRow
        |> pkgchk.reporting.Console.table

    let severitySettingsTable severities =
        formatSeverities severities
        |> ReportTable.singleRow
        |> pkgchk.reporting.Console.table

    let hitSummaryRow (value: ScaHitSummary) =
        let fmtSeverity kind severity =
            match kind with
            | ScaHitKind.VulnerabilityTransitive
            | ScaHitKind.Vulnerability -> colouriseSeverity severity
            | ScaHitKind.Deprecated -> colouriseReason severity
            | ScaHitKind.Dependency
            | ScaHitKind.DependencyTransitive -> severity
            | x -> failwith $"Unrecognised value {x}"

        let fmtCount value =
            match value with
            | 1 -> $"{value} hit"
            | _ -> $"{value} hits"

        [| Rendering.formatHitKind value.kind
           fmtSeverity value.kind value.severity
           fmtCount value.count |]

    let hitSummaryTable (counts: seq<ScaHitSummary>) =
        let table =
            { ReportTable.empty with
                columns = [ "Kind"; "Severity"; "Counts" ]
                rows = counts |> Seq.map (hitSummaryRow >> List.ofSeq) |> List.ofSeq }

        pkgchk.reporting.Console.table table


    let metadataLicenceDetails (metadata: PackageMetadata) =
        seq {
            metadata.License |> Option.nullDefault ""

            metadata.LicenseUrl
            |> Option.ofNull
            |> Option.map _.ToString()
            |> Option.defaultValue ""
        }
        |> Seq.filter String.isNotEmpty
        |> String.join Environment.NewLine
        |> Markup.Escape

    let metadataPackageDetails (metadata: PackageMetadata) =
        seq {
            nugetLinkPkgVsn metadata.Id metadata.Version |> lightcyan
            metadata.Description |> Markup.Escape |> lightgrey |> italic
        }
        |> String.join Environment.NewLine

    let metadataAuthors (metadata: PackageMetadata) =
        metadata.Authors |> Markup.Escape |> cyan |> italic

    let metadataProject (metadata: PackageMetadata) =
        metadata.ProjectUrl
        |> Option.ofNull
        |> Option.map _.ToString()
        |> Option.defaultValue ""
        |> Markup.Escape
        |> green

    let metadataReadme (metadata: PackageMetadata) =
        metadata.ReadmeUrl
        |> Option.ofNull
        |> Option.map _.ToString()
        |> Option.defaultValue ""
        |> Markup.Escape
        |> green

    let metadataTags (metadata: PackageMetadata) =
        metadata.Tags |> Markup.Escape |> grey |> italic

    let packageMetadataTableRows (metadata: PackageMetadata) =

        [ [ "Package"; metadataPackageDetails metadata ]
          if metadata.Authors <> "" then
              [ grey "Authors"; metadataAuthors metadata ]

          let licenceLines = metadataLicenceDetails metadata

          if licenceLines <> "" then
              [ grey "Licence"; licenceLines |> yellow ]

          if metadata.ProjectUrl |> Option.ofNull |> Option.isSome then
              [ grey "Project"; metadataProject metadata ]

          if metadata.ReadmeUrl |> Option.ofNull |> Option.isSome then
              [ grey "Readme"; metadataReadme metadata ]

          if metadata.Tags <> "" then
              [ grey "Tags"; metadataTags metadata ] ]

    let metadataSingleTable (metadata: PackageMetadata) =

        let rows =
            [ let message =
                  match metadata.Deprecation |> Option.ofNull with
                  | Some deprecation ->

                      seq {
                          ":warning:  This version is deprecated." |> error

                          if deprecation.Description <> "" then
                              deprecation.Description |> italic |> lightgrey

                          if deprecation.AlternatePackage |> Option.ofNull |> Option.isSome then
                              sprintf
                                  "Consider using %s %s instead."
                                  deprecation.AlternatePackage.Name
                                  deprecation.AlternatePackage.Range
                              |> Markup.Escape
                              |> yellow
                      }
                      |> String.join Environment.NewLine
                  | _ -> green ":check_mark_button: The package is not deprecated."

              [ grey "Deprecation"; message ]

              let message =
                  match metadata.Vulnerabilities |> List.ofSeq with
                  | [] -> green ":check_mark_button: No vulnerabilities found."
                  | xs ->
                      seq {
                          ":warning:  This version has known vulnerabilities." |> error

                          yield!
                              xs
                              |> Seq.sortByDescending (fun v -> v.Severity)
                              |> Seq.map (fun v -> $"{v.Severity.ToString() |> error} {v.AdvisoryUrl |> yellow}")
                      }
                      |> String.join Environment.NewLine

              [ grey "Vulnerabilities"; message ] ]



        let table =
            { ReportTable.empty with
                columns = [ ""; "" ]
                rows = packageMetadataTableRows metadata @ rows }

        table |> pkgchk.reporting.Console.table

    let metadataVersionsTable (versions: PackageMetadata[]) =

        let metadata =
            match versions |> Seq.filter (fun v -> v.IsPrerelease |> not) |> Seq.tryHead with
            | Some v -> v
            | None -> versions |> Seq.head

        let versionRows =
            versions
            |> Seq.rev
            |> Seq.map (fun v ->
                let lines =
                    seq {
                        let mutable safe = true

                        match (v.Published.HasValue, v.IsPrerelease) with
                        | (true, true) ->
                            $"Published {v.Published.Value.Date:``yyyy-MM-dd``}"
                            + (yellow " Prerelease version")
                        | (true, false) -> $"Published {v.Published.Value.Date:``yyyy-MM-dd``}"
                        | (false, true) -> yellow "Prerelease version"
                        | _ -> ""

                        if v.Deprecation |> Option.ofNull |> Option.isSome then
                            error ":warning:  This version is deprecated."
                            safe <- false

                        if v.Vulnerabilities |> Seq.isEmpty |> not then
                            error ":warning:  This version has known vulnerabilities."
                            safe <- false

                        if safe then
                            green ":check_mark_button: No known vulnerabilities or deprecations."

                    }
                    |> String.join Environment.NewLine

                let vsn = nugetLinkPkgVsnOnly metadata.Id v.Version |> cyan
                [ vsn; lines ])
            |> List.ofSeq

        let table =
            { ReportTable.empty with
                columns = [ ""; "" ]
                rows = packageMetadataTableRows metadata @ ([ "Versions"; "" ] :: versionRows) }

        table |> pkgchk.reporting.Console.table

    let packageScanTable (scans: PackageAutomationProperty[]) =
        match scans with
        | [||] ->
            { ReportTable.empty with
                columns = [ green ":check_mark_button: No package automation found." ]
                showHeaders = true }

        | scans ->
            { ReportTable.empty with
                columns = [ orange ":warning:  Package automation found."; "" ]
                showHeaders = true
                rows = scans |> Seq.map (fun s -> [ cyan s.propertyType; yellow s.path ]) |> List.ofSeq }
        |> pkgchk.reporting.Console.table
