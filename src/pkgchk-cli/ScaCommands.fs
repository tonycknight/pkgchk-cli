namespace pkgchk

open System
open FSharp.Data

type ScaVulnerabilityData = JsonProvider<"ScaVulnerabilitySample.json">
type ScaPackageTreeData = JsonProvider<"PackageDependencyTreeSample.json">

type ScaCommandContext =
    { trace: (string -> unit)
      projectPath: string
      includeVulnerabilities: bool
      includeTransitives: bool
      includeDeprecations: bool
      includeDependencies: bool
      includeOutdated: bool }

module ScaCommandParsing =

    let parseError (parseable: string) (ex: Exception) =
        match
            parseable.Split(Environment.NewLine)
            |> Seq.filter (fun s -> s.StartsWith("error:"))
            |> Array.ofSeq
        with
        | [||] -> ex.Message
        | xs -> xs |> String.join Environment.NewLine

    let parseVulnerabilities json =

        try
            let r = ScaVulnerabilityData.Parse(json)

            let topLevelVuls =
                r.Projects
                |> Seq.collect (fun p ->
                    p.Frameworks
                    |> Seq.collect (fun f ->
                        f.TopLevelPackages
                        |> Seq.collect (fun tp ->
                            tp.Vulnerabilities
                            |> Seq.map (fun v ->
                                { ScaHit.projectPath = System.IO.Path.GetFullPath(p.Path)
                                  kind = ScaHitKind.Vulnerability
                                  framework = f.Framework
                                  packageId = tp.Id
                                  resolvedVersion = tp.ResolvedVersion
                                  severity = v.Severity
                                  reasons = [||]
                                  suggestedReplacement = ""
                                  alternativePackageId = ""
                                  advisoryUri = v.Advisoryurl }))))

            let topLevelDeprecations =
                r.Projects
                |> Seq.collect (fun p ->
                    p.Frameworks
                    |> Seq.collect (fun f ->
                        f.TopLevelPackages
                        |> Seq.filter (fun tp -> tp.DeprecationReasons |> Array.isEmpty |> not)
                        |> Seq.map (fun tp ->
                            { ScaHit.projectPath = System.IO.Path.GetFullPath(p.Path)
                              kind = ScaHitKind.Deprecated
                              framework = f.Framework
                              packageId = tp.Id
                              resolvedVersion = tp.ResolvedVersion
                              severity = ""
                              suggestedReplacement =
                                match tp.AlternativePackage with
                                | Some ap -> sprintf "%s %s" ap.Id ap.VersionRange
                                | None -> ""
                              alternativePackageId =
                                match tp.AlternativePackage with
                                | Some ap -> ap.Id
                                | None -> ""
                              reasons = tp.DeprecationReasons |> Array.ofSeq

                              advisoryUri = "" })))

            let transitiveVuls =
                r.Projects
                |> Seq.collect (fun p ->
                    p.Frameworks
                    |> Seq.collect (fun f ->
                        f.TransitivePackages
                        |> Seq.collect (fun tp ->
                            tp.Vulnerabilities
                            |> Seq.map (fun v ->
                                { ScaHit.projectPath = System.IO.Path.GetFullPath(p.Path)
                                  kind = ScaHitKind.VulnerabilityTransitive
                                  framework = f.Framework
                                  packageId = tp.Id
                                  resolvedVersion = tp.ResolvedVersion
                                  severity = v.Severity
                                  reasons = [||]
                                  suggestedReplacement = ""
                                  alternativePackageId = ""
                                  advisoryUri = v.Advisoryurl }))))

            let hits =
                topLevelDeprecations
                |> Seq.append transitiveVuls
                |> Seq.append topLevelVuls
                |> List.ofSeq

            Choice1Of2 hits
        with ex ->
            parseError json ex |> Choice2Of2

    let parsePackageTree json =

        try
            let r = ScaPackageTreeData.Parse(json)

            let topLevelVuls =
                r.Projects
                |> Seq.collect (fun p ->
                    p.Frameworks
                    |> Seq.collect (fun f ->
                        f.TopLevelPackages
                        |> Seq.map (fun tp ->

                            { ScaHit.projectPath = System.IO.Path.GetFullPath(p.Path)
                              kind = ScaHitKind.Dependency
                              framework = f.Framework
                              packageId = tp.Id
                              resolvedVersion = tp.ResolvedVersion
                              severity = ""
                              reasons =
                                match tp.LatestVersion with
                                | Some _ -> [| "Upgrade available" |]
                                | _ -> [||]
                              suggestedReplacement = tp.LatestVersion |> Option.defaultValue ""
                              alternativePackageId = ""
                              advisoryUri = "" })))

            let transitiveVuls =
                r.Projects
                |> Seq.collect (fun p ->
                    p.Frameworks
                    |> Seq.collect (fun f ->
                        f.TransitivePackages
                        |> Seq.map (fun tp ->
                            { ScaHit.projectPath = System.IO.Path.GetFullPath(p.Path)
                              kind = ScaHitKind.DependencyTransitive
                              framework = f.Framework
                              packageId = tp.Id
                              resolvedVersion = tp.ResolvedVersion
                              severity = ""
                              reasons = [||]
                              suggestedReplacement = ""
                              alternativePackageId = ""
                              advisoryUri = "" })))

            let hits = transitiveVuls |> Seq.append topLevelVuls |> List.ofSeq

            Choice1Of2 hits
        with ex ->
            Choice2Of2("An error occurred parsing results." + Environment.NewLine)

module ScaCommandArgs =
    let private commandArgs vulnerable deprecated outdated includeTransitive path =
        sprintf
            "%s %s %s %s"
            (sprintf "list %s package" path)
            (match (vulnerable, deprecated, outdated) with
             | (true, _, _) -> "--vulnerable"
             | (_, true, _) -> "--deprecated"
             | (_, _, true) -> "--outdated"
             | (false, false, false) -> "")
            (match includeTransitive with
             | true -> "--include-transitive"
             | _ -> "")
            "--format json --output-version 1"

    let scanVulnerabilities = commandArgs true false false

    let scanDeprecations = commandArgs false true false

    let scanDependencies = commandArgs false false false

    let scanOutdated = commandArgs false false true false

    let restoreArgs projectPath =
        projectPath |> Io.fullPath |> sprintf "restore %s -nowarn:NU1510"

    let scanArgs (context: ScaCommandContext) =
        let projPath = context.projectPath |> Io.fullPath

        [| if context.includeVulnerabilities then
               yield
                   (projPath |> scanVulnerabilities context.includeTransitives, ScaCommandParsing.parseVulnerabilities)
           if context.includeDeprecations then
               yield (projPath |> scanDeprecations context.includeTransitives, ScaCommandParsing.parseVulnerabilities)
           if context.includeDependencies then
               yield (projPath |> scanDependencies context.includeTransitives, ScaCommandParsing.parsePackageTree)
           if context.includeOutdated then
               yield (scanOutdated projPath, ScaCommandParsing.parsePackageTree) |]
