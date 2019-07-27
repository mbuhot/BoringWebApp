namespace BoringWebApp.Orders
open Microsoft.AspNetCore.Mvc
open System

[<CLIMutable>]
type OrderResponse =
    {
        OrderId: int
        CreatedAt: DateTime
        Customer: string
        DiscountCode: string
        Status: string
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

