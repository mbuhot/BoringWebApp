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

let private flip f x y = f y x

let rec fold (folder: 'State -> 'T -> 'State Task) (state: 'State) (elems: 'T list): 'State Task =
    match elems with
    | [] -> Task.FromResult state
    | (head :: tail) -> task {
        let! state = folder state head
        return! fold folder state tail
    }
