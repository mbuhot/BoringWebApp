namespace BoringWebApp.Orders

open System
open System.Threading.Tasks
open Microsoft.AspNetCore.Mvc
open FSharp.Control.Tasks.V2

open System.ComponentModel.DataAnnotations
open BoringWebApp

/// <summary>
/// Create a new Order, initializing with the Customer
/// </summary>
[<CLIMutable>]
type CreateOrderRequest =
    {
        /// <summary> The customer ID </summary>
        [<Required>]
        [<MaxLength(255)>]
        Customer: string
    }

/// <summary>
/// Add an item to an order
/// </summary>
[<CLIMutable>]
type AddItemRequest =
    {
        /// <summary>
        /// The ID of the order to add the item to
        /// </summary>
        [<Required>]
        OrderId: int

        /// <summary>
        /// The product ID the order item will refer to
        /// </summary>
        [<Required>]
        [<MaxLength(255)>]
        Product: string

        /// <summary>
        /// The initial quantity of Product for the item
        /// </summary>
        [<Required>]
        Quantity: int
    }

/// <summary>
/// Operations for listing, creating and updating Orders
/// </summary>
[<ApiController>]
type OrdersController (repo: OrderRepository) =
    inherit ControllerBase()

    /// <summary>
    /// Lists all Orders
    /// </summary>
    /// <remarks>This is a more long-winded way to say: Lists all orders</remarks>
    /// <response code="200">The lists of Orders</response>
    [<HttpGet("api/orders/", Name="Orders.Index")>]
    [<ProducesResponseType(200)>]
    member __.Index() : Task<ActionResult<OrderResponse[]>> =
        repo.AllOrders()
        |> Task.bind repo.IncludeItems
        |> Task.map OrdersView.index


    /// <summary>
    /// Get an Order By ID
    /// </summary>
    /// <remarks>The response will include the list of OrderItems if any exist</remarks>
    /// <param name="orderId">The ID of the Order</param>
    /// <response code="200">The Order</response>
    [<HttpGet("api/orders/{orderId}", Name="Orders.Show")>]
    [<ProducesResponseType(200)>]
    member __.Show(orderId: int) : Task<ActionResult<OrderResponse>> =
        orderId
        |> repo.FindOrderById
        |> Task.bind repo.IncludeItems
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
        |> Task.bind repo.IncludeItems
        |> Task.map (OrderService.addItem request.Product request.Quantity 13.0M)
        |> Task.bind repo.AddItem
        |> Task.map OrdersView.itemAdded

    [<HttpDelete("api/orders/{orderId}/items/{orderItemId}", Name="Orders.RemoveItem")>]
    [<ProducesResponseType(204)>]
    member __.RemoveItem(orderId: int, orderItemId: int) =
        task {
            let! item = repo.FindOrderItemById orderItemId
            if item.OrderId = orderId then
                let event = OrderService.removeItem item
                do! repo.DeleteItem event
                return NoContentResult() :> ActionResult
            else
                return NotFoundResult() :> ActionResult
        }
