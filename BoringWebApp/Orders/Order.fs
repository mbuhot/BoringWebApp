namespace BoringWebApp.Orders
open System

type Order =
    {
        // fields
        OrderId: int
        CreatedAt: DateTime
        Customer: string
        DiscountCode: string option
        Status: string

        //relationships
        OrderItems: OrderItem list option
    }

and OrderItem =
    {
        // fields
        OrderItemId: int
        OrderId: int
        Product: string
        UnitPrice: decimal
        Quantity: int

        // relationships
        Order: Order option
    }

type OrderCreated =
    {
        Order: Order
    }

type ItemAdded =
    {
        OrderId: int
        Product: string
        UnitPrice: decimal
        Quantity: int
    }

type ItemQuantityChanged =
    {
        OrderItemId: int
        Product: string
        Quantity: int
    }

type ItemRemoved =
    {
        OrderItemId: int
    }

type OrderSubmitted =
    {
        OrderId: int
        Status: string
    }
