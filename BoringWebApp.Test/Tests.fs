module Tests

open FSharp.Control.Tasks.V2
open System.Threading.Tasks

open Xunit
open BoringWebApp.Controllers

type HttpClient = System.Net.Http.HttpClient
type WebApplicationFactory<'a when 'a: not struct> = Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<'a>

[<CLIMutable>]
type MyResponseType = {
    Value: string
}


module HttpClient =
    open Microsoft.AspNetCore.Components
    let postJsonAsync (path: string) (body: 'a) (client: HttpClient): Task<'b> =
        client.PostJsonAsync<'b>(path, body)

    let getJsonAsync (path: string) (client: HttpClient): Task<'a> =
        client.GetJsonAsync<'a>(path)

    let getStringAsync (path: string) (client: HttpClient): Task<string> =
        client.GetStringAsync(path)

module Should =
    let equal (actual: 'a) (expected: 'a) =
        Assert.Equal<'a>(expected, actual)

module WebApplicationFactory =
    let service<'a> (factory: WebApplicationFactory<_>) : 'a =
        factory.Services.GetService(typeof<'a>) :?> 'a

    let client (factory: WebApplicationFactory<_>) : HttpClient =
        factory.CreateClient()

type ValuesTest(factory: WebApplicationFactory<BoringWebApp.Startup>) =
    let valuesRoutes : ValuesRouteHelpers = factory |> WebApplicationFactory.service
    let client = factory |> WebApplicationFactory.client

    interface IClassFixture<WebApplicationFactory<BoringWebApp.Startup>>

    [<Fact>]
    member this.IndexValues() = task {
        let! response = client |> HttpClient.getJsonAsync valuesRoutes.IndexPath
        response |> Should.equal [|"value1"; "value2"|]
    }

    [<Fact>]
    member this.ShowValue() = task {
        let! response = client |> HttpClient.getStringAsync (valuesRoutes.ShowPath 123)
        response |> Should.equal "value"
    }

    [<Fact>]
    member this.CreateValue() = task {
        let! response = client |> HttpClient.postJsonAsync valuesRoutes.CreatePath { Value = "Hello" }
        response |> Should.equal {Value = "12345"}
    }
