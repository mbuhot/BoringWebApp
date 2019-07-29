namespace BoringWebApp.Orders
open System
open System.ComponentModel.DataAnnotations
open System.ComponentModel.DataAnnotations.Schema

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

[<Table("orders")>]
type OrderCreated =
    {
        [<Column("created_at")>]
        CreatedAt: DateTime

        [<Column("customer")>]
        Customer: string

        [<Column("discount_code")>]
        DiscountCode: string option

        [<Column("status")>]
        Status: string
    }

[<Table("order_items")>]
type ItemAdded =
    {
        [<Column("order_id")>]
        OrderId: int

        [<Column("product")>]
        Product: string

        [<Column("unit_price")>]
        UnitPrice: decimal

        [<Column("quantity")>]
        Quantity: int
    }

[<Table("order_items")>]
type ItemQuantityChanged =
    {
        [<Key>]
        [<Column("order_item_id")>]
        OrderItemId: int

        [<Column("product")>]
        Product: string

        [<Column("quantity")>]
        Quantity: int
    }

[<Table("order_items")>]
type ItemRemoved =
    {
        [<Key>]
        [<Column("order_item_id")>]
        OrderItemId: int
    }

[<Table("orders")>]
type OrderSubmitted =
    {
        [<Key>]
        [<Column("order_id")>]
        OrderId: int

        [<Column("status")>]
        Status: string
    }
