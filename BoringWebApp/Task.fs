module BoringWebApp.Task

open System.Threading.Tasks
open FSharp.Control.Tasks.V2

let compose (f: 'a -> Task<'b>) (g: 'b -> Task<'c>) : ('a -> Task<'c>) =
    fun (a: 'a) ->
        ContextInsensitive.task {
            let! b = f a
            let! c = g b
            return c
        }

let bind (f: 'a -> Task<'b>) (a: Task<'a>) : Task<'b> =
    task {
        let! a' = a
        return! f a'
    }

let map (f: 'a -> 'b) (t: Task<'a>) : Task<'b> =
    task {
        let! a = t
        return f a
    }
