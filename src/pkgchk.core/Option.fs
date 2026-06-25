namespace pkgchk

open System
open System.Diagnostics

module Option =

    [<DebuggerStepThrough>]
    let nullDefault<'a> (defaultValue: 'a) (value: 'a) =
        if obj.ReferenceEquals(value, null) then
            defaultValue
        else
            value

    [<DebuggerStepThrough>]
    let isNull<'a> (value: 'a) = obj.ReferenceEquals(value, null)

    [<DebuggerStepThrough>]
    let ofNull<'a> (value: 'a) =
        if obj.ReferenceEquals(value, null) then
            None
        else
            Some value
