namespace pkgchk

module Rendering =

    [<Literal>]
    let nugetPrefix = "https://www.nuget.org/packages"

    [<Literal>]
    let white = "white"

    [<Literal>]
    let lightgrey = "#909090"

    [<Literal>]
    let green = "lime"

    [<Literal>]
    let cyan = "cyan"

    [<Literal>]
    let yellow = "yellow"

    [<Literal>]
    let orange = "##f57a51"

    [<Literal>]
    let cornflowerblue = "#6495ed"

    [<Literal>]
    let red = "red"

    [<Literal>]
    let grey = "grey"

    let formatHitKind =
        function
        | ScaHitKind.VulnerabilityTransitive -> "Vulnerable transitive"
        | ScaHitKind.Vulnerability -> "Vulnerable package"
        | ScaHitKind.Deprecated -> "Deprecated package"
        | ScaHitKind.Dependency -> "Dependency"
        | ScaHitKind.DependencyTransitive -> "Transitive Dependency"


    let maxHitKindLength () =
        [ ScaHitKind.VulnerabilityTransitive
          ScaHitKind.Vulnerability
          ScaHitKind.Deprecated ]
        |> Seq.map (formatHitKind >> (fun s -> s.Length))
        |> Seq.max

    let reasonColour =
        function
        | "Critical Bugs" -> red
        | "Legacy" -> yellow
        | _ -> cyan

    let severityColour =
        function
        | "High" -> red
        | "Critical" -> red
        | "Critical Bugs" -> red
        | "Moderate" -> orange
        | _ -> yellow

    let severityStyle =
        function
        | "Critical" -> "italic"
        | _ -> ""

    let severityEmote =
        function
        | "Critical" -> ":bangbang:"
        | "Moderate"
        | "" -> ""
        | "High"
        | _ -> ":heavy_exclamation_mark:"

    let nugetLink (package, version) =
        match package, version with
        | p, "" -> $"{nugetPrefix}/{p}"
        | p, v -> $"{nugetPrefix}/{p}/{v}"
