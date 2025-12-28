namespace pkgchk

open System
open YamlDotNet.Serialization

[<CLIMutable>]
type ScanConfiguration =
    { noBanner: Nullable<bool>
      noRestore: Nullable<bool>
      includePackages: string[]
      excludePackages: string[]
      breakOnUpgrades: Nullable<bool>
      severities: string[]
      scanVulnerabilities: Nullable<bool>
      scanDeprecations: Nullable<bool>
      scanTransitives: Nullable<bool> }

module Config =
    let private deserialiser = (new DeserializerBuilder()).Build()

    let load (path: string) =
        use reader = new System.IO.StreamReader(path)
        let content = reader.ReadToEnd()
        deserialiser.Deserialize<ScanConfiguration>(content)
