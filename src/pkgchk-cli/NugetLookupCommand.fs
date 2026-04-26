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

                return m |> Option.ofNull
            with ex ->
                return None
        }


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
                        [ Console.metadataVersionsTable versions ] |> CliCommands.renderTables
                        CliCommands.returnCode true
            else
                let! metadata = metadata settings.PackageId settings.PackageVersion

                match metadata with
                | None -> return CliCommands.returnError "The package metadata was not found."

                | Some m ->
                    [ Console.metadataSingleTable m ] |> CliCommands.renderTables

                    return CliCommands.returnCode true
        }
