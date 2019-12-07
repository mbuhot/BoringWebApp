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

/// <summary>
/// Specifies the Sort order for an Order query
/// </summary>
type OrderSort =
    | SortById
    | SortByCreatedAt
    | SortByStatus
    | SortByCustomer

/// <summary>
/// Specifies a Filter to be applied to an Order
/// </summary>
type OrderFilter =
   | WithId of {| OrderId: int |}
   | CreatedBetween of {| MinCreatedAt: DateTime; MaxCreatedAt: DateTime |}
   | WithCustomer of {| Customer: string |}
   | WithStatus of {| Status: string |}

/// <summary>
/// Specifies related data to include with an Order query
/// </summary>
type OrderIncludes =
    | IncludeItems of OrderItemIncludes list
    | IncludeCustomer of CustomerIncludes list

/// <summary>
/// Specifies related data to include with an OrderItem query
/// </summary>
and OrderItemIncludes =
    | IncludeProductInfo

/// <summary>
/// Specifies related data to include
/// </summary>
and CustomerIncludes =
    | IncludeRecentPurchases


/// <summary>
/// A dynamically generated Order query
/// </summary>
type OrderQuery =
    {
        /// <summary>
        /// Filters constrain the Orders that are returned
        /// </summary>
        Filters: OrderFilter list

        /// <summary>
        /// Results will be ordered by the list of Sort options given
        /// </summary>
        Sort: OrderSort list

        /// <summary>
        /// Related to be included
        /// </summary>
        Includes: OrderIncludes list
    }
    static member All =
        { Filters = []; Sort = []; Includes = [] }

    static member Where(filter: OrderFilter) =
        { Filters = [filter]; Sort = []; Includes = [] }

    static member Include (including: OrderIncludes) (query: OrderQuery) =
        { query with Includes = including :: query.Includes }

    static member WithId (orderId: int) =
        OrderQuery.Where <| WithId {| OrderId = orderId |}

    static member IncludeItems (query: OrderQuery) =
        query |> OrderQuery.Include (OrderIncludes.IncludeItems [])

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

type OrderEvent =
    | OrderCreated of OrderCreated
    | ItemAdded of ItemAdded
    | ItemQuantityChanged of ItemQuantityChanged
    | ItemRemoved of ItemRemoved
    | OrderSubmitted of OrderSubmitted
