namespace pkgchk

open YamlDotNet.Serialization

[<CLIMutable>]
type Configuration =
    {
        includedPackages: string[]
        excludedPackages: string[]
    }

module Config =
    let private deserialiser = (new DeserializerBuilder()).Build()

    let load (path: string) =         
        use reader = new System.IO.StreamReader(path)
        let content = reader.ReadToEnd()
        let r = deserialiser.Deserialize<Configuration>(content) 
        { r with includedPackages = r.includedPackages |> Option.nullDefault [||]; 
                    excludedPackages = r.excludedPackages |> Option.nullDefault [||] 
        }
        
