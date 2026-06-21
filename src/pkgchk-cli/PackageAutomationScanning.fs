namespace pkgchk

open System
open Tk.Nuget

module PackageAutomationScanning =
    let private findHits patterns name path =
        patterns
        |> Seq.collect (Io.findFiles path)
        |> Seq.map (fun p ->
            { name = Io.fileName p
              path = Io.relativePath path p
              fullPath = p
              propertyType = name })

    let private scanPackageElements (path: string) =
        seq {
            findHits [ "./build/*.props" ] "Build property"
            findHits [ "./build/*.targets" ] "Build target"
            findHits [ "*.ps1" ] "Powershell script"
            findHits [ "*.cmd"; "*.sh" ] "Shell script"
            findHits [ "*.js" ] "Javascript file"
            findHits [ "./tools/" ] "Tool"
            findHits [ "./analyzers/*.dll" ] "Analyzer"
            findHits [ "./buildtransitive/*.targets" ] "Build Transitive target"
            findHits [ "./buildtransitive/*.props" ] "Build Transitive property"
        }
        |> Seq.collect (fun f -> f path)
        |> Seq.distinctBy (fun s -> s.path)


    let scanPackage (nuget: INugetClient) name version outputDir =
        task {

            let mutable path = ""
            let useTempPath = String.IsNullOrWhiteSpace outputDir

            try
                if String.IsNullOrWhiteSpace outputDir then
                    path <- Io.tempDirectoryPath () |> Io.randomDirectory
                    path <- path |> Io.createDirectory |> _.FullName                    
                else
                    path <- Io.normalise outputDir

                let! packagePath = nuget.DownloadNugetPackageAsync(name, version, path, true)
                
                return scanPackageElements packagePath |> Array.ofSeq

            finally
                if useTempPath && path <> ""  then
                    path |> Exception.iter Io.deleteDirectory ignore
        }
