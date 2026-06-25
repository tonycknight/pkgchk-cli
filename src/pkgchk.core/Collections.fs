namespace pkgchk

open System.Collections.Generic
open System.Diagnostics

module HashSet =

    [<DebuggerStepThrough>]
    let ofSeq<'a> (comparer: IEqualityComparer<'a>) (values: seq<'a>) = new HashSet<'a>(values, comparer)

    [<DebuggerStepThrough>]
    let contains<'a> (hashSet: HashSet<'a>) = hashSet.Contains
