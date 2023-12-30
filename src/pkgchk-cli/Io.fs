namespace pkgchk

open System
open System.Diagnostics.CodeAnalysis
open System.IO

module Io =

    [<ExcludeFromCodeCoverage>]
    let toFullPath (path: string) =
        if not <| Path.IsPathRooted(path) then
            let wd = Environment.CurrentDirectory

            Path.Combine(wd, path)
        else
            path
