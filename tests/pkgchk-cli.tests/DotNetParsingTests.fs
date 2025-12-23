namespace pkgchk.tests

open Xunit
open FsUnit.Xunit

module DotNetParsingTests =

    let getFile filename =
        let assembly = System.Reflection.Assembly.GetExecutingAssembly()
        let nspace = assembly.GetExportedTypes().[0].Namespace
        assembly.GetManifestResourceStream($"{nspace}.{filename}")


    [<Theory>]
    [<InlineData(" ")>]
    [<InlineData("ABC")>]
    let ``parseVulnerabilities of plain text`` (text) =
        match text |> pkgchk.DotNetParsing.parseVulnerabilities with
        | Choice2Of2 msg -> ignore 0
        | _ -> failwith "No error raised"

    [<Fact>]
    let ``parseVulnerabilities for empty results`` () =

        use f = getFile "ScaSampleEmpty.json"

        use reader = new System.IO.StreamReader(f)

        let r = reader.ReadToEnd() |> pkgchk.DotNetParsing.parseVulnerabilities

        match r with
        | Choice1Of2 xs ->
            match xs with
            | [] -> ignore 0
            | _ -> failwith "Unrecognised list returned"
        | _ -> failwith "No error raised"


    [<Fact>]
    let ``parseVulnerabilities for vulnerabilities`` () =

        use f = getFile "ScaSampleWithVulnerabilities.json"

        use reader = new System.IO.StreamReader(f)

        let r = reader.ReadToEnd() |> pkgchk.DotNetParsing.parseVulnerabilities

        match r with
        | Choice1Of2 xs ->
            match xs with
            | [] -> failwith "Empty list returned"
            | [ x; y ] ->
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

    [<Theory>]
    [<InlineData(" ")>]
    [<InlineData("ABC")>]
    let ``parsePackageTree of plain text`` (text) =
        match text |> pkgchk.DotNetParsing.parsePackageTree with
        | Choice2Of2 msg -> ignore 0
        | _ -> failwith "No error raised"

    [<Fact>]
    let ``parsePackageTree for empty results`` () =

        use f = getFile "ScaSampleEmpty.json"

        use reader = new System.IO.StreamReader(f)

        let r = reader.ReadToEnd() |> pkgchk.DotNetParsing.parsePackageTree

        match r with
        | Choice1Of2 xs ->
            match xs with
            | [] -> ignore 0
            | _ -> failwith "Unrecognised list returned"
        | _ -> failwith "No error raised"


    [<Fact>]
    let ``parsePackageTree for dependencies`` () =

        use f = getFile "PackageDependencyTreeSample.json"

        use reader = new System.IO.StreamReader(f)

        let r = reader.ReadToEnd() |> pkgchk.DotNetParsing.parsePackageTree

        match r with
        | Choice1Of2 xs ->
            match xs with
            | [] -> failwith "Empty list returned"
            | x :: t when t |> List.length = 229 ->
                x.framework |> should equal "net7.0"
                x.packageId |> should equal "FSharp.Core"
                x.resolvedVersion |> should equal "8.0.100"
                x.severity |> should equal ""
                x.advisoryUri |> should equal ""

            | _ -> failwith "Unrecognised list returned"
        | _ -> failwith "No error raised"
