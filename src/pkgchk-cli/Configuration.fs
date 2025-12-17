namespace pkgchk

open YamlDotNet.Serialization

[<CLIMutable>]
type ScanConfiguration =
    { noBanner: bool
      includedPackages: string[]
      excludedPackages: string[]
      breakOnChanges: bool
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
