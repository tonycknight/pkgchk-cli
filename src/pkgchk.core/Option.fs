namespace pkgchk

module Option =
    let nullDefault<'a> (defaultValue: 'a) (value: 'a) =
        if obj.ReferenceEquals(value, null) then
            defaultValue
        else
            value

    let isNull<'a> (value: 'a) = obj.ReferenceEquals(value, null)

    let ofNull<'a> (value: 'a) =
        if obj.ReferenceEquals(value, null) then
            None
        else
            Some value
