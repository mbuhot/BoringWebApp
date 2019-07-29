namespace BoringWebApp.Orders
open Microsoft.AspNetCore.Mvc
open System
open System.ComponentModel.DataAnnotations

/// <summary>
/// An Order
/// </summary>
[<CLIMutable>]
type OrderResponse =
    {
        /// <summary>
        /// The unique identifier
        /// </summary>
        [<Required>]
        OrderId: int

        /// <summary>
        /// The instant the order was created
        /// </summary>
        [<Required>]
        CreatedAt: DateTime

        /// <summary>
        /// The unique ID of the customer that created the Order
        /// </summary>
        [<Required>]
        [<MaxLength(255)>]
        Customer: string

        /// <summary>
        /// An optional discount code to apply to the Order
        /// </summary>
        [<MaxLength(255)>]
        DiscountCode: string

        /// <summary>
        /// The order status, one of "NEW", "Complete"
        /// </summary>
        [<Required>]
        [<MaxLength(32)>]
        Status: string

        /// <summary>
        /// The individual product items in the Order
        /// </summary>
        [<Required>]
        OrderItems: OrderItemResponse[]
    }

and [<CLIMutable>] OrderItemResponse =
    {
        OrderItemId: int
        Product: string
        UnitPrice: decimal
        Quantity: int
    }

[<CLIMutable>]
type CreateResponse =
    {
        Id: int
    }

module OrdersView =
    let renderOrderItem (orderItem: OrderItem) : OrderItemResponse =
        {
            OrderItemId = orderItem.OrderItemId
            Product = orderItem.Product
            UnitPrice = orderItem.UnitPrice
            Quantity = orderItem.Quantity
        }

    let renderOrder (order: Order) : OrderResponse =
        {
            OrderId = order.OrderId
            CreatedAt = order.CreatedAt
            Customer = order.Customer
            DiscountCode = order.DiscountCode |> Option.defaultValue null
            Status = order.Status
            OrderItems =
                order.OrderItems
                |> Option.map ((List.map renderOrderItem) >> Array.ofList)
                |> Option.defaultValue null
        }

    let index (orders: Order seq) : ActionResult<OrderResponse[]> =
        orders
        |> Seq.map (renderOrder)
        |> Seq.toArray
        |> ActionResult<OrderResponse[]>

    let show (order: Order) : ActionResult<OrderResponse> =
        order
        |> renderOrder
        |> ActionResult<OrderResponse>

    let created (orderId: int) : ActionResult<CreateResponse> =
        CreatedAtActionResult(
            actionName = "Show",
            controllerName = "Orders",
            routeValues = {|OrderId=orderId|},
            value = {CreateResponse.Id = orderId}
        )
        |> ActionResult<CreateResponse>

    let itemAdded (orderItemId: int) : ActionResult<CreateResponse> =
        ActionResult<CreateResponse>({ Id = orderItemId })

