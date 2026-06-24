namespace pkgchk

open System.Text.Json.Serialization
open System.Text.Json

module Json =

    let private options () =
        let opts =
            JsonFSharpOptions
                .Default()
                .WithUnionUnwrapFieldlessTags()
                .WithMapFormat(MapFormat.Object)
                .ToJsonSerializerOptions()

        opts.Converters.Add(new JsonStringEnumConverter())
        opts.WriteIndented <- true
        opts

    let serialise<'a> =
        let opts = options ()
        fun (value: 'a) -> JsonSerializer.Serialize(value, opts)

    let deserialise<'a> =
        let opts = options ()
        fun (value: string) -> JsonSerializer.Deserialize<'a>(value, opts)
