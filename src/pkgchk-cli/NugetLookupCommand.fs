namespace pkgchk

open System
open System.Diagnostics.CodeAnalysis
open Spectre.Console
open Spectre.Console.Cli
open Tk.Nuget

[<ExcludeFromCodeCoverage>]
type NugetLookupCommand(nuget: INugetClient) =
    inherit AsyncCommand<NugetLookupCommandSettings>()

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
            metadata.LicenseUrl |> Option.ofNull |> Option.map _.ToString() |> Option.defaultValue ""
        }
        |> Seq.filter String.isNotEmpty
        |> String.join Environment.NewLine

    let metadataTable (metadata: PackageMetadata) =
        let table = 
            Console.table()
            |> Console.tableColumn ""
            |> Console.tableColumn ""

        table.AddRow [| Console.grey "Package"; sprintf "%s %s" (metadata.Id |> Console.lightcyan) (metadata.Version |> Console.yellow) |] |> ignore        
        table.AddRow [| ""; metadata.Description |> Console.lightgrey |> Console.italic |] |> ignore
        
        if metadata.Authors <> "" then
            table.AddRow [| Console.grey "Authors"; metadata.Authors |> Console.cyan |> Console.italic |] |> ignore
                    
        let licenceLines = licenceDetails metadata            
        if licenceLines <> "" then           
            table.AddRow [| Console.grey "Licence"; licenceLines |> Console.yellow |] |> ignore
        
        if metadata.ProjectUrl |> Option.ofNull |> Option.isSome then
            table.AddRow [| Console.grey "Project"; metadata.ProjectUrl |> Option.ofNull |> Option.map _.ToString() |> Option.defaultValue "" |> Console.green |] |> ignore

        if metadata.ReadmeUrl |> Option.ofNull |> Option.isSome then
            table.AddRow [| Console.grey "Readme"; metadata.ReadmeUrl |> Option.ofNull |> Option.ofNull |> Option.map _.ToString() |> Option.defaultValue "" |> Console.green |] |> ignore

        if metadata.Tags <> "" then
            table.AddRow [| Console.grey "Tags"; metadata.Tags |> Console.grey |> Console.italic |] |> ignore

        let deprecation = metadata.Deprecation |> Option.ofNull
        if deprecation |> Option.isSome then                        
            let lines = 
                seq { 
                    "This package is deprecated." |> Console.error 
                    deprecation.Value.Description |> Console.italic |> Console.lightgrey
                    if deprecation.Value.AlternatePackage |> Option.ofNull |> Option.isSome then
                        sprintf "Consider using %s %s instead." 
                            deprecation.Value.AlternatePackage.Name 
                            deprecation.Value.AlternatePackage.Range |> Markup.Escape |> Console.yellow
                }
                |> String.join Environment.NewLine                

            table.AddRow [| Console.grey "Deprecation"; lines |] |> ignore
                    
        if metadata.Vulnerabilities |> Seq.isEmpty |> not then
            
            let message = 
                seq {
                    "This package has known vulnerabilities." |> Console.error
                    yield! metadata.Vulnerabilities 
                            |> Seq.sortByDescending (fun v -> v.Severity)
                            |> Seq.map (fun v -> $"{v.Severity.ToString() |> Console.error} {v.AdvisoryUrl |> Console.yellow}")
                } 
                |> String.join Environment.NewLine

            table.AddRow [| Console.grey "Vulnerabilities"; message  |] |> ignore
            
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

            let! metadata = metadata settings.PackageId settings.PackageVersion

            match metadata with
            | None -> 
                return CliCommands.returnError "The package metadata was not found."
                
            | Some m ->
                [ metadataTable m ] |> CliCommands.renderTables

                return CliCommands.returnCode true
        }