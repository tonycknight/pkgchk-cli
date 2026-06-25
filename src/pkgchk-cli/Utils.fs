namespace pkgchk

open System
open System.Diagnostics
open System.Diagnostics.CodeAnalysis

module ReturnCodes =

    [<Literal>]
    let validationOk = 0

    [<Literal>]
    let validationFailed = 1

    [<Literal>]
    let sysError = 2
