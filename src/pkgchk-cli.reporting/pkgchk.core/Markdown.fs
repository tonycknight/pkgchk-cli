namespace pkgchk

module Markdown =
    
    let italic (value: string) = $"_{value}_"

    let escape (value: string) = value.Replace('\r', ' ').Replace('\n', ' ')

    let append (separator: string) (x: string) (y: string) =
        if y.Length = 0 then x
        else if x.Length = 0 then y
        else $"{y}{separator}{x}"

    let colourise colour value =
        $"<span style='color:{colour}'>{value}</span>"

    let hyperlink name url = $"[{name}]({url})"

    let link url = hyperlink url url

    let image uri = $"![image]({uri})"