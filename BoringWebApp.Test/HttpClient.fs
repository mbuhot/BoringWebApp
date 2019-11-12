/// Helpers for working with HttpClient and F# type inference in tests
module BoringWebApp.Test.HttpClient

open System.Net.Http
open System.Threading.Tasks

open BoringWebApp


let postJsonAsync (path: string) (body: 'a) (client: HttpClient): Task<'b> =
    use body = new StringContent(System.Text.Json.JsonSerializer.Serialize body)
    body.Headers.ContentType.MediaType <- "application/json"
    client.PostAsync(path, body)
    |> Task.bind (fun x -> x.Content.ReadAsStringAsync())
    |> Task.map (fun x -> System.Text.Json.JsonSerializer.Deserialize x)

let putJsonAsync (path: string) (body: 'a) (client: HttpClient): Task<'b> =
    use body = new StringContent(System.Text.Json.JsonSerializer.Serialize body)
    body.Headers.ContentType.MediaType <- "application/json"
    client.PutAsync(path, body)
    |> Task.bind (fun x -> x.Content.ReadAsStringAsync())
    |> Task.map (fun x -> System.Text.Json.JsonSerializer.Deserialize x)

let getJsonAsync (path: string) (client: HttpClient): Task<'a> =
    client.GetAsync(path)
    |> Task.bind (fun x -> x.Content.ReadAsStringAsync())
    |> Task.map (fun x -> System.Text.Json.JsonSerializer.Deserialize x)

let getStringAsync (path: string) (client: HttpClient): Task<string> =
    client.GetStringAsync(path)
