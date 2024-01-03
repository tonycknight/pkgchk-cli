namespace pkgchk

module Rendering =

    let formatHitKind =
        function
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
        | "High" -> ":bangbang:"
        | "Critical" -> ":heavy_exclamation_mark:"
        | "Moderate" -> ":heavy_exclamation_mark:"
        | "" -> ""
        | _ -> ":heavy_exclamation_mark:"

    let nugetLink (package, version) =
        match package, version with
        | p, "" -> $"https://www.nuget.org/packages/{p}"
        | p, v -> $"https://www.nuget.org/packages/{p}/{v}"

    