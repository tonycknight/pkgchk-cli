namespace pkgchk

open pkgchk.Combinators
open YamlDotNet.Serialization

[<CLIMutable>]
type ScanConfiguration =
    { noBanner: bool
      noRestore: bool
      includedPackages: string[]
      excludedPackages: string[]
      breakOnUpgrades: bool
      severities: string[]
      breakOnVulnerabilities: bool
      breakOnDeprecations: bool
      checkTransitives: bool }

module Config =
    let private deserialiser = (new DeserializerBuilder()).Build()

    let load (path: string) =
        use reader = new System.IO.StreamReader(path)
        let content = reader.ReadToEnd()
        let r = deserialiser.Deserialize<ScanConfiguration>(content)

        { r with
            includedPackages = r.includedPackages |> Option.nullDefault [||]
            excludedPackages = r.excludedPackages |> Option.nullDefault [||]
            severities = r.severities |> Option.nullDefault [||] }

    let filterPackages (config: ScanConfiguration) (hits: pkgchk.ScaHit list) =
        let inclusionMap =
            config.includedPackages
            |> HashSet.ofSeq System.StringComparer.InvariantCultureIgnoreCase

        let exclusionMap =
            config.excludedPackages
            |> HashSet.ofSeq System.StringComparer.InvariantCultureIgnoreCase

        let included (hit: ScaHit) =
            match inclusionMap.Count with
            | 0 -> true
            | x -> inclusionMap.Contains hit.packageId

        let excluded (hit: ScaHit) =
            match exclusionMap.Count with
            | 0 -> true
            | x -> exclusionMap.Contains hit.packageId |> not

        hits |> List.filter (included &&>> excluded)
