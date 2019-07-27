namespace BoringWebApp.Test

open System
open FSharp.Control.Tasks.V2
open Microsoft.Extensions.DependencyInjection

open Xunit

open BoringWebApp.Values


type ValuesTests() =
    static let factory = new BoringWebApplicationFactory()
    let txn = factory.DbConnection.BeginTransaction()
    let routes = factory.Services.GetRequiredService<ValuesRouteHelpers>()
    let client = factory.CreateClient()

    interface IDisposable with member this.Dispose() = txn.Dispose()

    [<Fact>]
    member this.``Index lists all created values``() = task {
        let! _ = client |> HttpClient.postJsonAsync routes.Create { Value = "value1" }
        let! _ = client |> HttpClient.postJsonAsync routes.Create { Value = "value2" }
        let! (response: BoringValue[]) = client |> HttpClient.getJsonAsync routes.Index
        response |> Array.map (fun x -> x.Value) |> Should.equal [|"value1"; "value2"|]
    }

    [<Fact>]
    member this.``Show displays a single value``() = task {
        let! (response: BoringValue) = client |> HttpClient.postJsonAsync routes.Create { Value = "Hello" }
        let! (response: BoringValue) = client |> HttpClient.getJsonAsync (routes.Show response.Id)
        response.Value |> Should.equal "Hello"
    }

    [<Fact>]
    member this.``Create persists a single Value``() = task {
        let! (response: IntId) = client |> HttpClient.postJsonAsync routes.Create { Value = "Hello" }
        response.Id |> Should.beGreaterThan 0
    }

    [<Fact>]
    member this.``Update modified a single Value``() = task {
        let! (response: IntId) = client |> HttpClient.postJsonAsync routes.Create { Value = "Hello" }
        let! (response: BoringValue) = client |> HttpClient.postJsonAsync (routes.Update response.Id) { Value = "Hello2" }
        response.Id |> Should.beGreaterThan 0
    }
