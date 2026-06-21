namespace pkgchk

open System
open Tk.Nuget

module PackageScanning =        
    let private findHits patterns name path =        
        patterns
            |> Seq.collect (Io.findFiles path)            
            |> Seq.map (fun p -> { name = Io.fileName p; path = Io.relativePath path p; propertyType = name })
            |> Array.ofSeq

    let private scanBuildProps = findHits [ "./build/*.props" ] "Build property"
    let private scanBuildTargets = findHits [ "./build/*.targets" ] "Build target"
    let private scanTools = findHits [ "./tools/" ] "Tool"
    let private scanPowershell = findHits [ "*.ps1" ] "Powershell script"
    let private scanShellScripts = findHits [ "*.cmd"; "*.sh" ] "Shell script"
    let private scanJavascriptScripts = findHits [ "*.js" ] "Javascript file"
    let private scanAnalyzers = findHits [ "./analyzers/*.dll"] "Analyzer"
    let private scanBuildTransitiveTargets = findHits [ "./buildtransitive/*.targets" ] "Build Transitive target"
    let private scanBuildTransitiveProps = findHits [ "./buildtransitive/*.props" ] "Build Transitive property"
     
    let private scanPackageElements (path: string) =
        [
            scanBuildProps
            scanBuildTargets
            scanPowershell
            scanShellScripts
            scanJavascriptScripts
            scanTools
            scanAnalyzers
            scanBuildTransitiveTargets
            scanBuildTransitiveProps
        ]
        |> Seq.collect (fun f -> f path)
        |> Seq.distinctBy (fun s -> s.path)

    let scanPackage (nuget: INugetClient) name version =
        task {

            let mutable path = ""

            try
                path <- Io.tempDirectoryPath () |> Io.randomDirectory
                path <- path |> Io.createDirectory |> _.FullName

                let! packagePath = nuget.DownloadNugetPackageAsync(name, version, path, true)

                let hits = scanPackageElements packagePath

                return hits |> Array.ofSeq

            finally
                if path <> "" then
                    path |> Exception.iter Io.deleteDirectory ignore
        }