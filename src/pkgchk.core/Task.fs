namespace pkgchk

open System.Threading.Tasks

module Task =
    let ofResult<'a> (value: 'a) = Task.FromResult value
    let result<'a> (value: Task<'a>) = value.Result

    let iter (tasks: Task<'a> seq) =
        task {
            let mutable results = []

            for task in tasks do
                let! r = task
                results <- r :: results

            return results
        }
