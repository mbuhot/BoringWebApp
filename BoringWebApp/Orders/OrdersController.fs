namespace BoringWebApp.Orders

open System
open System.Threading.Tasks
open Microsoft.AspNetCore.Mvc
open FSharp.Control.Tasks.V2

open BoringWebApp

[<CLIMutable>]
type CreateOrderRequest =
    {
        Customer: string
    }

[<CLIMutable>]
type AddItemRequest =
    {
        OrderId: int
        Product: string
        Quantity: int
    }

[<ApiController>]
type OrdersController (repo: OrderRepository) =
    inherit ControllerBase()

    [<HttpGet("api/orders/", Name="Orders.Index")>]
    [<ProducesResponseType(200)>]
    member __.Index() : Task<ActionResult<OrderResponse[]>> =
        repo.AllOrdersWithItems()
        |> Task.map OrdersView.index

    [<HttpGet("api/orders/{orderId}", Name="Orders.Show")>]
    [<ProducesResponseType(200)>]
    member __.Show(orderId: int) : Task<ActionResult<OrderResponse>> =
        orderId
        |> repo.FindOrderById
        |> Task.bind repo.LoadItems
        |> Task.map OrdersView.show

    [<HttpPost("api/orders/", Name="Orders.Create")>]
    [<ProducesResponseType(201)>]
    member this.Create([<FromBody>] request: CreateOrderRequest) : Task<ActionResult<CreateResponse>> =
        OrderService.createOrder request.Customer DateTime.UtcNow
        |> repo.Insert
        |> Task.map OrdersView.created

    [<HttpPost("api/orders/{orderId}/items/", Name="Orders.AddItem")>]
    [<ProducesResponseType(201)>]
    member this.AddItem(request: AddItemRequest) : Task<ActionResult<CreateResponse>> =
        request.OrderId
        |> repo.FindOrderById
        |> Task.bind repo.LoadItems
        |> Task.map (OrderService.addItem request.Product request.Quantity 13.0M)
        |> Task.bind repo.AddItem
        |> Task.map OrdersView.itemAdded

    [<HttpDelete("api/orders/{orderId}/items/{orderItemId}", Name="Orders.RemoveItem")>]
    [<ProducesResponseType(204)>]
    member __.RemoveItem(orderId: int, orderItemId: int) =
        task {
            let! item = repo.LoadItemWithOrder orderId orderItemId
            let event = OrderService.removeItem item
            do! repo.DeleteItem event
            return NoContentResult()
        }
