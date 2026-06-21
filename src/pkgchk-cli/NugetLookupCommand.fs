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
            try
                let! versions = nuget.GetAllMetadataAsync(name, prerelease)

                return versions |> Array.ofSeq
            with ex ->
                return [||]
        }

    let metadata name version =
        task {
            try
                let! m =
                    match version with
                    | "" -> nuget.GetLatestMetadataAsync(name)
                    | _ -> nuget.GetMetadataAsync(name, version)

                return
                    match m |> Option.ofNull with
                    | Some m -> [| m |]
                    | None -> [||]
            with ex ->
                return [||]
        }

    let scanPackage name version =
        task {
            
            let mutable path = ""
            try
                path <- Io.tempDirectoryPath () |> Io.randomDirectory
                path <- path |> Io.createDirectory |> _.FullName
                                
                let! packagePath = nuget.DownloadNugetPackageAsync(name, version, path, true)
                
                // TODO: scan it

                return [||]
                    
            finally
                if path <> "" then
                    path |> Exception.iter Io.deleteDirectory ignore
        }

    let versions (settings: NugetLookupCommandSettings) =
        match settings.AllVersions with
        | true -> versions settings.PackageId settings.PreRelease
        | false -> metadata settings.PackageId settings.PackageVersion

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

            let mutable metadata: PackageMetadata[] = [||]
            let mutable packageScan = [||]

            do!
                AnsiConsole
                    .Status()
                    .Spinner(Spinner.Known.Dots12)
                    .StartAsync(
                        "Looking up package metadata...",
                        fun ctx ->
                            task {
                                let! xs = versions settings
                                metadata <- xs

                                if settings.ScanPackage && metadata |> Array.isEmpty |> not then
                                    ctx.Status("Scanning package...") |> ignore
                                    let vsn = metadata |> Array.last

                                    let! xs = scanPackage settings.PackageId vsn.Version
                                    packageScan <- xs
                            }
                    )

            return
                match metadata with
                | [||] -> CliCommands.returnError "The package metadata was not found."
                | xs ->
                    match settings.AllVersions with
                    | true ->
                        [ Console.metadataVersionsTable metadata ] |> CliCommands.renderTables
                        CliCommands.returnCode true
                    | false ->
                        [ Console.metadataSingleTable (Array.head xs) ] |> CliCommands.renderTables
                        CliCommands.returnCode true

        }
