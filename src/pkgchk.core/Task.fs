namespace pkgchk

open System.Threading.Tasks

module Task =
    let ofResult<'a> (value: 'a) = Task.FromResult value
