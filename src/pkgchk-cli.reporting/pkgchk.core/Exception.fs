namespace pkgchk

open System

module Exception =
    let iter (func: 'a -> unit) (exHandler: Exception -> unit) (value: 'a) =
        try
            func value
        with ex ->
            exHandler ex
