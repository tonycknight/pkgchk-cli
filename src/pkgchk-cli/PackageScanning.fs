namespace pkgchk

open System
open Tk.Nuget

module PackageScanning =
    let private findHits patterns name path =
        patterns
        |> Seq.collect (Io.findFiles path)
        |> Seq.map (fun p ->
            { name = Io.fileName p
              path = Io.relativePath path p
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
