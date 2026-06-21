namespace pkgchk.tests

open FsCheck.Xunit

module ExceptionTest =

    [<Property>]
    let ``iter when exception raised, the handler is triggered`` (value: string) =
        let mutable triggerd = false

        value |> pkgchk.Exception.iter invalidOp (fun x -> triggerd <- true)

        triggerd

    [<Property>]
    let ``iter when no exception raised, the handler is not triggered`` (value: string) =
        let mutable triggerd = false

        value |> pkgchk.Exception.iter ignore (fun x -> triggerd <- true)

        not triggerd
