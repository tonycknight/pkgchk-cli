namespace pkgchk

open System
open System.Diagnostics.CodeAnalysis
open Spectre.Console
open Spectre.Console.Cli
open Tk.Nuget

[<ExcludeFromCodeCoverage>]
type NugetLookupCommand(nuget: INugetClient) =
    inherit AsyncCommand<NugetLookupCommandSettings>()

    let versions name prerelease =
        task {

            let! versions = nuget.GetAllMetadataAsync(name, prerelease)

            return versions |> Array.ofSeq
        }

    let metadata name version =
        task {
            try
                let! m =
                    match version with
                    | "" -> nuget.GetLatestMetadataAsync(name)
                    | _ -> nuget.GetMetadataAsync(name, version)

                return m |> Option.ofNull
            with ex ->
                return None
        }

    let licenceDetails (metadata: PackageMetadata) =
        seq {
            metadata.License |> Option.nullDefault ""

            metadata.LicenseUrl
            |> Option.ofNull
            |> Option.map _.ToString()
            |> Option.defaultValue ""
        }
        |> Seq.filter String.isNotEmpty
        |> String.join Environment.NewLine

    let packageDetails (metadata: PackageMetadata) =
        seq {
            sprintf "%s %s" (metadata.Id |> Console.lightcyan) (metadata.Version |> Console.yellow)
            metadata.Description |> Console.lightgrey |> Console.italic
        }
        |> String.join Environment.NewLine

    let authors (metadata: PackageMetadata) =
        metadata.Authors |> Console.cyan |> Console.italic

    let project (metadata: PackageMetadata) =
        metadata.ProjectUrl
        |> Option.ofNull
        |> Option.map _.ToString()
        |> Option.defaultValue ""
        |> Console.green

    let readme (metadata: PackageMetadata) =
        metadata.ReadmeUrl
        |> Option.ofNull
        |> Option.ofNull
        |> Option.map _.ToString()
        |> Option.defaultValue ""
        |> Console.green

    let tags (metadata: PackageMetadata) =
        metadata.Tags |> Console.grey |> Console.italic

    let metadataTable (metadata: PackageMetadata) =
        let table = Console.table () |> Console.tableColumn "" |> Console.tableColumn ""

        table.AddRow [| "Package"; packageDetails metadata |] |> ignore

        if metadata.Authors <> "" then
            table.AddRow [| Console.grey "Authors"; authors metadata |] |> ignore

        let licenceLines = licenceDetails metadata

        if licenceLines <> "" then
            table.AddRow [| Console.grey "Licence"; licenceLines |> Console.yellow |]
            |> ignore

        if metadata.ProjectUrl |> Option.ofNull |> Option.isSome then
            table.AddRow [| Console.grey "Project"; project metadata |] |> ignore

        if metadata.ReadmeUrl |> Option.ofNull |> Option.isSome then
            table.AddRow [| Console.grey "Readme"; readme metadata |] |> ignore

        if metadata.Tags <> "" then
            table.AddRow [| Console.grey "Tags"; tags metadata |] |> ignore

        let deprecation = metadata.Deprecation |> Option.ofNull

        if deprecation |> Option.isSome then
            let lines =
                seq {
                    "This package is deprecated." |> Console.error
                    deprecation.Value.Description |> Console.italic |> Console.lightgrey

                    if deprecation.Value.AlternatePackage |> Option.ofNull |> Option.isSome then
                        sprintf
                            "Consider using %s %s instead."
                            deprecation.Value.AlternatePackage.Name
                            deprecation.Value.AlternatePackage.Range
                        |> Markup.Escape
                        |> Console.yellow
                }
                |> String.join Environment.NewLine

            table.AddRow [| Console.grey "Deprecation"; lines |] |> ignore

        if metadata.Vulnerabilities |> Seq.isEmpty |> not then

            let message =
                seq {
                    "This package has known vulnerabilities." |> Console.error

                    yield!
                        metadata.Vulnerabilities
                        |> Seq.sortByDescending (fun v -> v.Severity)
                        |> Seq.map (fun v ->
                            $"{v.Severity.ToString() |> Console.error} {v.AdvisoryUrl |> Console.yellow}")
                }
                |> String.join Environment.NewLine

            table.AddRow [| Console.grey "Vulnerabilities"; message |] |> ignore

        table

    let versionsTable (versions: PackageMetadata[]) =
        let table = Console.table () |> Console.tableColumn "" |> Console.tableColumn ""

        let metadata =
            match versions |> Seq.filter (fun v -> v.IsPrerelease |> not) |> Seq.tryHead with
            | Some v -> v
            | None -> versions |> Seq.head

        // emit the details of this version to describe the package as a whole
        table.AddRow [| "Package"; packageDetails metadata |] |> ignore

        if metadata.Authors <> "" then
            table.AddRow [| Console.grey "Authors"; authors metadata |] |> ignore

        let licenceLines = licenceDetails metadata

        if licenceLines <> "" then
            table.AddRow [| Console.grey "Licence"; licenceLines |> Console.yellow |]
            |> ignore

        if metadata.ProjectUrl |> Option.ofNull |> Option.isSome then
            table.AddRow [| Console.grey "Project"; project metadata |] |> ignore

        if metadata.ReadmeUrl |> Option.ofNull |> Option.isSome then
            table.AddRow [| Console.grey "Readme"; readme metadata |] |> ignore

        if metadata.Tags <> "" then
            table.AddRow [| Console.grey "Tags"; tags metadata |] |> ignore

        // now enumerate the versions
        table.AddRow [| "Versions"; "" |] |> ignore

        versions
        |> Seq.rev
        |> Seq.iter (fun v ->
            let lines =
                seq {
                    let mutable safe = true

                    if v.IsPrerelease then
                        "Prerelease version" |> Console.yellow

                    if v.Deprecation |> Option.ofNull |> Option.isSome then
                        ":warning:  This version is deprecated." |> Console.error
                        safe <- false

                    if v.Vulnerabilities |> Seq.isEmpty |> not then
                        ":warning:  This version has known vulnerabilities." |> Console.error
                        safe <- false

                    if safe then
                        ":check_mark_button: No known vulnerabilities or deprecations." |> Console.green

                }
                |> String.join Environment.NewLine

            table.AddRow [| Console.cyan v.Version; lines |] |> ignore)

        table


    override _.Validate
        (context: CommandContext, settings: NugetLookupCommandSettings)
        : Spectre.Console.ValidationResult =

        if String.IsNullOrWhiteSpace(settings.PackageId) then
            Spectre.Console.ValidationResult.Error("Package ID is required.")
        else
            settings.Validate()

    override _.ExecuteAsync(context, settings, cancellationToken) =
        task {
            if settings.NoBanner |> not then
                CliCommands.renderBanner nuget

            if settings.AllVersions then
                let! versions = versions settings.PackageId settings.PreRelease

                return
                    match versions with
                    | [||] -> CliCommands.returnError "The package metadata was not found."
                    | _ ->
                        // TODO: the array is in descending version order
                        // which to pick for the headline info? last non-prerelease?
                        [ versionsTable versions ] |> CliCommands.renderTables
                        CliCommands.returnCode true
            else
                let! metadata = metadata settings.PackageId settings.PackageVersion

                match metadata with
                | None -> return CliCommands.returnError "The package metadata was not found."

                | Some m ->
                    [ metadataTable m ] |> CliCommands.renderTables

                    return CliCommands.returnCode true
        }
