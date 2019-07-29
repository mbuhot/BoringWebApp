module BoringWebApp.Orders.OrderService
open System

let createOrder (customer: string) (now: DateTime) : OrderCreated =
    let event: OrderCreated = {
        CreatedAt = now
        Customer = customer
        DiscountCode = None
        Status = "New"
    }
    event

let addItem (product: string) (quantity: int) (price: decimal) (order: Order) : ItemAdded =
    if order.OrderItems.IsNone then failwith "OrderItems must be loaded"
    if order.OrderItems.Value |> List.exists (fun x -> x.Product = product) then failwith "Product already in Order"

    let event : ItemAdded = {
        OrderId = order.OrderId
        Product = product
        UnitPrice = price
        Quantity = quantity
    }
    event

let removeItem (item: OrderItem) : ItemRemoved =
    if item.Order.Value.Status <> "New" then failwith "Can only remove from New order"
    let event : ItemRemoved = {
        OrderItemId = item.OrderItemId
    }
    event
