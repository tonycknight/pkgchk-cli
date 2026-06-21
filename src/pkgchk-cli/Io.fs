namespace pkgchk

open System
open System.Diagnostics.CodeAnalysis
open System.IO

[<ExcludeFromCodeCoverage>]
module Io =

    let combine name path = Path.Combine(path, name)
            
    let fullPath (path: string) =
        if not <| Path.IsPathRooted(path) then
            let wd = Environment.CurrentDirectory

            Path.Combine(wd, path)
        else
            path

    let normalise (path: string) = Path.GetFullPath(path)

    let tempDirectoryPath () =
        Path.GetTempPath() |> combine "pkgchk-cli" |> fullPath

    let randomDirectory (path: string) =
        let guid = Guid.NewGuid().ToString("N")
        path |> combine guid |> fullPath

    let writeFile (path: string) (lines: seq<string>) =
        let dir = Path.GetDirectoryName path
        Directory.CreateDirectory dir |> ignore
        File.WriteAllText(path, lines |> String.joinLines)
        path

    let createDirectory directory =
        if not <| Directory.Exists(directory) then
            Directory.CreateDirectory(directory)
        else
            DirectoryInfo(directory)

    let deleteDirectory (path: string) =        
        if Directory.Exists(path) then
            Directory.Delete(path, true) 
        
    let composeFilePath directory fileName =
        let file = fullPath >> combine fileName >> normalise
        file directory
