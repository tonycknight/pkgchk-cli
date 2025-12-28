namespace pkgchk

open System
open YamlDotNet.Serialization

[<CLIMutable>]
type ScanConfiguration =
    { noBanner: Nullable<bool>
      noRestore: Nullable<bool>
      includedPackages: string[]
      excludedPackages: string[]
      breakOnUpgrades: Nullable<bool>
      severities: string[]
      scanVulnerabilities: Nullable<bool>
      scanDeprecations: Nullable<bool>
      checkTransitives: Nullable<bool> }

module Config =
    let private deserialiser = (new DeserializerBuilder()).Build()

    let load (path: string) =
        use reader = new System.IO.StreamReader(path)
        let content = reader.ReadToEnd()
        deserialiser.Deserialize<ScanConfiguration>(content)
        