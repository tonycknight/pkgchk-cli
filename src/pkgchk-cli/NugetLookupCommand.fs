namespace pkgchk

open System
open System.Diagnostics.CodeAnalysis
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

            let! versions = versions settings

            return
                match versions with
                | [||] -> CliCommands.returnError "The package metadata was not found."
                | xs ->
                    match settings.AllVersions with
                    | true ->
                        [ Console.metadataVersionsTable versions ] |> CliCommands.renderTables
                        CliCommands.returnCode true
                    | false ->
                        [ Console.metadataSingleTable (Array.head xs) ] |> CliCommands.renderTables
                        CliCommands.returnCode true

        }
