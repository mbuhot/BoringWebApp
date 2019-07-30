namespace BoringWebApp.Test

open System
open FSharp.Control.Tasks.V2
open Microsoft.Extensions.DependencyInjection

open Xunit

open BoringWebApp.Orders
open Xunit.Abstractions


type OrdersTests(_output: ITestOutputHelper) =
    static let factory = new BoringWebApplicationFactory()
    let txn = factory.DbConnection.BeginTransaction()
    let routes = factory.Services.GetRequiredService<OrdersRouteHelpers>()
    let client = factory.CreateClient()

    interface IDisposable with member this.Dispose() = txn.Dispose()

    [<Fact>]
    member this.``Index lists all created Orders``() = task {
        let! _ = client |> HttpClient.postJsonAsync routes.Create { Customer = "Henry" }
        let! _ = client |> HttpClient.postJsonAsync routes.Create { Customer = "Percy" }
        let! _ = client |> HttpClient.postJsonAsync routes.Create { Customer = "James" }
        let! (response: OrderResponse[]) = client |> HttpClient.getJsonAsync routes.Index
        response |> Array.map (fun x -> x.Customer) |> Should.equal [|"Henry"; "Percy"; "James"|]
    }

    [<Fact>]
    member this.``Show displays a single Order``() = task {
        let! (response: CreateResponse) = client |> HttpClient.postJsonAsync routes.Create { Customer = "Mike" }
        response.Id |> Should.beGreaterThan 0

        let! (response: OrderResponse) = client |> HttpClient.getJsonAsync (routes.Show response.Id)
        response.Customer |> Should.equal "Mike"
    }

    [<Fact>]
    member this.``AddItem to Order``() = task {
        let! (response: CreateResponse) = client |> HttpClient.postJsonAsync routes.Create { Customer = "Mike" }
        let orderId = response.Id
        orderId |> Should.beGreaterThan 0

        let newItem = {
            OrderId = orderId
            Product = "Television"
            Quantity = 12
        }
        let! (response: CreateResponse) = client |> HttpClient.postJsonAsync (routes.AddItem orderId) newItem
        response.Id |> Should.beGreaterThan 0

        let! (response: OrderResponse) = client |> HttpClient.getJsonAsync (routes.Show orderId)
        response.OrderItems |> Array.exactlyOne |> (fun x -> x.Product) |> Should.equal "Television"
    }

//    [<Fact>]
//    member this.``Update modified a single Value``() = task {
//        let! (response: IntId) = client |> HttpClient.postJsonAsync routes.Create { Value = "Hello" }
//        let! (response: BoringValue) = client |> HttpClient.postJsonAsync (routes.Update response.Id) { Value = "Hello2" }
//        response.Id |> Should.beGreaterThan 0
//    }
