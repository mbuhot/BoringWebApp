/// Helpers for working with HttpClient and F# type inference in tests
module BoringWebApp.Test.HttpClient

open System.Net.Http
open System.Text.Json
open System.Threading.Tasks
open FSharp.Control.Tasks.V2
open Microsoft.AspNetCore.Components


let postJsonAsync (path: string) (body: 'a) (client: HttpClient): Task<'b> =
    client.PostJsonAsync<'b>(path, body)

let putJsonAsync (path: string) (body: 'a) (client: HttpClient): Task<'b> =
    client.PutJsonAsync<'b>(path, body)

let getJsonAsync (path: string) (client: HttpClient): Task<'a> =
    client.GetJsonAsync<'a> path

let getStringAsync (path: string) (client: HttpClient): Task<string> =
    client.GetStringAsync(path)
