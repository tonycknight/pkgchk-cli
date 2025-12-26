namespace pkgchk

open System
open System.Diagnostics.CodeAnalysis
open System.IO

[<ExcludeFromCodeCoverage>]
module Io =

    let private combine name path = System.IO.Path.Combine(path, name)

    let fullPath (path: string) =
        if not <| Path.IsPathRooted(path) then
            let wd = Environment.CurrentDirectory

            Path.Combine(wd, path)
        else
            path

    let normalise (path: string) = System.IO.Path.GetFullPath(path)

    let writeFile (path: string) (lines: seq<string>) =
        let dir = System.IO.Path.GetDirectoryName path
        System.IO.Directory.CreateDirectory dir |> ignore
        System.IO.File.WriteAllText(path, lines |> String.join Environment.NewLine)
        path

    let composeFilePath directory fileName =
        let file = fullPath >> combine fileName >> normalise
        file directory
