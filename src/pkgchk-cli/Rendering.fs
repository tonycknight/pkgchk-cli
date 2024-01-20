namespace pkgchk

module Rendering =

    [<Literal>]
    let nugetPrefix = "https://www.nuget.org/packages"

    let formatHitKind =
        function
        | ScaHitKind.VulnerabilityTransitive -> "Vulnerable transitive"
        | ScaHitKind.Vulnerability -> "Vulnerable package"
        | ScaHitKind.Deprecated -> "Deprecated package"

    let reasonColour =
        function
        | "Critical Bugs" -> "red"
        | "Legacy" -> "yellow"
        | _ -> "cyan"

    let severityColour =
        function
        | "High" -> "red"
        | "Critical" -> "red"
        | "Moderate" -> "#d75f00"
        | _ -> "yellow"

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
