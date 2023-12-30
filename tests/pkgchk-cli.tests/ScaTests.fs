namespace pkgchk.tests

open FsUnit.Xunit
open Xunit

module ScaTests =

    let getFile filename =
        let assembly = System.Reflection.Assembly.GetExecutingAssembly()
        let nspace = assembly.GetExportedTypes().[0].Namespace
        assembly.GetManifestResourceStream($"{nspace}.{filename}")

    [<Theory>]
    [<InlineData(" ")>]
    [<InlineData("ABC")>]
    let ``parse of plain text`` (text) =
        match text |> pkgchk.Sca.parse with
        | Choice2Of2 msg -> ignore 0
        | _ -> failwith "No error raised"

    [<Fact>]
    let ``parse for empty results`` () =

        use f = getFile "ScaSampleEmpty.json"

        use reader = new System.IO.StreamReader(f)

        let r = reader.ReadToEnd() |> pkgchk.Sca.parse

        match r with
        | Choice1Of2 xs ->
            match xs with
            | [] -> ignore 0
            | _ -> failwith "Unrecognised list returned"
        | _ -> failwith "No error raised"


    [<Fact>]
    let ``parse for vulnerabilities`` () =

        use f = getFile "ScaSampleWithVulnerabilities.json"

        use reader = new System.IO.StreamReader(f)

        let r = reader.ReadToEnd() |> pkgchk.Sca.parse

        match r with
        | Choice1Of2 xs ->
            match xs with
            | [] -> failwith "Empty list returned"
            | [ y; x ] ->
                x.framework |> should equal "net7.0"
                x.packageId |> should equal "System.Net.Http"
                x.resolvedVersion |> should equal "4.3.0"
                x.severity |> should equal "Critical"
                x.advisoryUri |> should not' (be NullOrEmptyString)

                y.framework |> should equal "net7.0"
                y.packageId |> should equal "System.Text.RegularExpressions"
                y.resolvedVersion |> should equal "4.3.1"
                y.severity |> should equal "High"
                y.advisoryUri |> should not' (be NullOrEmptyString)
            | _ -> failwith "Unrecognised list returned"
        | _ -> failwith "No error raised"
