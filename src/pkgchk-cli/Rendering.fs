namespace pkgchk

module Rendering =

    [<Literal>]
    let nugetPrefix = "https://www.nuget.org/packages"

    let formatHitKind =
        function
        | ScaHitKind.VulnerabilityTransitive -> "Vulnerable transitive"
        | ScaHitKind.Vulnerability -> "Vulnerable package"
        | ScaHitKind.Deprecated -> "Deprecated package"
        | ScaHitKind.Dependency -> "Dependency"
        | ScaHitKind.DependencyTransitive -> "Transitive Dependency"
        | x -> failwith $"Unrecognised value {x}"


    let maxHitKindLength () =
        [ ScaHitKind.VulnerabilityTransitive
          ScaHitKind.Vulnerability
          ScaHitKind.Deprecated ]
        |> Seq.map (formatHitKind >> (fun s -> s.Length))
        |> Seq.max

    let reasonColour =
        function
        | "Critical Bugs" -> Colours.red
        | "Legacy" -> Colours.yellow
        | _ -> Colours.cyan

    let severityColour =
        function
        | "High" -> Colours.red
        | "Critical" -> Colours.red
        | "Critical Bugs" -> Colours.red
        | "Moderate" -> Colours.orange
        | _ -> Colours.yellow

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
