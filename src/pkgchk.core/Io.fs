namespace pkgchk

open System
open System.Diagnostics.CodeAnalysis
open System.IO

[<ExcludeFromCodeCoverage>]
module Io =

    let join (name: string) path = Path.Join(path, name)

    let fullPath (path: string) = Path.GetFullPath(path) 
    
    let fileName (path: string) = Path.GetFileName path

    let relativePath (rootPath: string) (path: string) = Path.GetRelativePath(rootPath, path)

    let tempDirectoryPath () =
        Path.GetTempPath() |> join "pkgchk-cli" |> fullPath

    let randomDirectory (path: string) =
        let guid = Guid.NewGuid().ToString("N")
        path |> join guid |> fullPath

    let writeFile (path: string) (lines: string seq) = 
        let dir = Path.GetDirectoryName path
        Directory.CreateDirectory dir |> ignore
        File.WriteAllText(path, lines |> String.joinLines)
        path

    let writeLinesAsync (lines: seq<string>) (path: string) = // TODO: reorder params?
        task {
            File.WriteAllLines(path, lines)
            return path
        }

    let createDirectory directory =
        if not <| Directory.Exists(directory) then
            Directory.CreateDirectory(directory)
        else
            DirectoryInfo(directory)

    let deleteDirectory (path: string) =
        if Directory.Exists(path) then
            Directory.Delete(path, true)

    let findFiles directory pattern =
        if Directory.Exists(directory) then
            try
                Directory.GetFiles(directory, pattern, SearchOption.AllDirectories)
            with :? System.IO.DirectoryNotFoundException ->
                [||]
        else
            [||]
