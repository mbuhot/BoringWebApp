namespace BoringWebApp.Orders
open System.Data
open System.Data.Common
open System.Threading.Tasks
open FSharp.Control.Tasks.V2
open BoringWebApp
open BoringWebApp.DataRecordHelpers

/// Defines the conversions between IDataRecord and Order domain types
module private Bindings =
    let recordToOrder (r: IDataRecord) : Order =
        {
            OrderId = r?order_id
            CreatedAt = r?created_at
            Customer = r?customer
            DiscountCode = r?discount_code
            Status = r?status
            OrderItems = None
        }

    let recordToOrderItem (order: Order option) (r: IDataRecord) : OrderItem =
        {
            OrderItemId = r?order_item_id
            OrderId = r?order_id
            Product = r?product
            UnitPrice = r?unit_price
            Quantity = r?quantity
            Order = order
        }


/// Defines the DB queries for loading Order Domain types by ids or other attributes
module private Queries =

    let allOrders (db: DbConnection) : Order list Task =
        Db.query "SELECT * FROM orders" [] Bindings.recordToOrder db

    let findOrderById (orderId: int) (db: DbConnection) : Order Task =
        Db.queryOne
            "SELECT * FROM orders WHERE order_id = @OrderId"
            (Db.parameters {|OrderId = orderId|})
            Bindings.recordToOrder
            db

    let findOrderItemById (orderItemId: int) (db: DbConnection) : OrderItem Task =
        Db.queryOne
            "SELECT * FROM order_items WHERE order_item_id = @OrderItemId"
            (Db.parameters {|OrderItemId = orderItemId|})
            (Bindings.recordToOrderItem None)
            db

    let findItemsForOrderIds (orderIds: int[]) (db: DbConnection): OrderItem list Task =
        Db.query
            """
            SELECT *
            FROM order_items
            WHERE order_id = ANY(@OrderIds)
            ORDER BY order_id asc, order_item_id asc
            """
            (Db.parameters {|OrderIds = orderIds|})
            (Bindings.recordToOrderItem None)
            db


/// Defines functions to load related data and attach to Order domain types
module private Relations =
    let includeItemsForOrders(orders: Order list) (db: DbConnection) : Order list Task =
        task {
            let! items = Queries.findItemsForOrderIds [|for o in orders -> o.OrderId|] db
            return
                query {
                    for o in orders do
                    groupJoin i in items on (o.OrderId = i.OrderId) into orderItems
                    select {o with OrderItems = orderItems |> List.ofSeq |> Some}
                } |> List.ofSeq
        }

    let includeItemsForOrder(order: Order) (db: DbConnection): Order Task =
        includeItemsForOrders [order] db |> Task.map (List.exactlyOne)

    let includeOrder (orderItem: OrderItem) (db: DbConnection): OrderItem Task =
        Queries.findOrderById orderItem.OrderId db
        |> Task.map (fun x -> {orderItem with Order = Some x})


/// Exposes a facade that can be injected into a Controller
type OrderRepository(db: DbConnection) =

    // Queries

    member this.AllOrders() : Order list Task =
        Queries.allOrders db

    member this.FindOrderById(orderId: int) : Order Task =
        Queries.findOrderById orderId db

    member this.FindOrderItemById (orderItemId: int) : OrderItem Task =
        Queries.findOrderItemById orderItemId db


    // Relations

    member this.IncludeItems(orders: Order list) : Order list Task =
        Relations.includeItemsForOrders orders db

    member this.IncludeItems(order: Order): Order Task =
        Relations.includeItemsForOrder order db

    member this.IncludeOrder (orderItem: OrderItem): OrderItem Task =
        Relations.includeOrder orderItem db


    // Mutations

    member this.Insert(event: OrderCreated) : int Task =
        db |> Db.insert event (fun r -> r?order_id)

    member this.AddItem(event: ItemAdded) : int Task =
        db |> Db.insert event (fun r -> r?order_item_id)

    member this.UpdateQuantity(event: ItemQuantityChanged) =
        db |> Db.updateByPrimaryKey event

    member this.UpdateStatus(event: OrderSubmitted) =
        db |> Db.updateByPrimaryKey event

    member this.DeleteItem(event: ItemRemoved) =
        db |> Db.deleteByPrimaryKey event
