/// Helpers for working with HttpClient and F# type inference in tests
module BoringWebApp.Test.HttpClient

open System.Net.Http
open System.Threading.Tasks

open System.Text.Json
open BoringWebApp

let jsonOptions =
    JsonSerializerOptions(PropertyNamingPolicy = JsonNamingPolicy.CamelCase)

let serialize x =
    System.Text.Json.JsonSerializer.Serialize(x, jsonOptions)

let deserialize (str: string) =
    System.Text.Json.JsonSerializer.Deserialize(str, jsonOptions)

let postJsonAsync (path: string) (body: 'a) (client: HttpClient): Task<'b> =
    use body = new StringContent(serialize body)
    body.Headers.ContentType.MediaType <- "application/json"
    client.PostAsync(path, body)
    |> Task.bind (fun x -> x.Content.ReadAsStringAsync())
    |> Task.map (fun x -> deserialize x)

let putJsonAsync (path: string) (body: 'a) (client: HttpClient): Task<'b> =
    use body = new StringContent(serialize body)
    body.Headers.ContentType.MediaType <- "application/json"
    client.PutAsync(path, body)
    |> Task.bind (fun x -> x.Content.ReadAsStringAsync())
    |> Task.map (fun x -> deserialize x)

let getJsonAsync (path: string) (client: HttpClient): Task<'a> =
    client.GetAsync(path)
    |> Task.bind (fun x -> x.Content.ReadAsStringAsync())
    |> Task.map (fun x -> deserialize x)

let getStringAsync (path: string) (client: HttpClient): Task<string> =
    client.GetStringAsync(path)
