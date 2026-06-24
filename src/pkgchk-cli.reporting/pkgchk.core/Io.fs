namespace pkgchk

open System
open System.Diagnostics.CodeAnalysis
open System.IO

[<ExcludeFromCodeCoverage>]
module Io =

    let combine (name: string) path = Path.Combine(path, name)

    let join (name: string) path = Path.Join(path, name)

    let fullPath (path: string) =
        if Path.IsPathRooted(path) |> not then
            join Environment.CurrentDirectory path
        else
            Path.GetFullPath(path)

    let tempDirectoryPath () =
        Path.GetTempPath() |> join "pkgchk-cli" |> fullPath

    let randomDirectory (path: string) =
        let guid = Guid.NewGuid().ToString("N")
        path |> combine guid |> fullPath

    let writeLinesAsync (lines: seq<string>) (path: string) =
        task {
            File.WriteAllLines(path, lines)
            return path
        }