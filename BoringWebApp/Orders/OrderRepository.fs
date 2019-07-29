namespace BoringWebApp.Orders
open System.Data
open System.Data.Common
open System.Threading.Tasks
open FSharp.Control.Tasks.V2
open BoringWebApp
open DataRecordHelpers

type OrderRepository(db: DbConnection) =
    ////
    // Record bindings

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

    ////
    // Queries

    member this.AllOrders() : Order list Task =
        Db.query "SELECT * FROM orders" [] recordToOrder db

    member this.FindOrderById(orderId: int) : Order Task =
        Db.queryOne
            "SELECT * FROM orders WHERE order_id = @OrderId"
            (Db.parameters {|OrderId = orderId|})
            recordToOrder
            db

    member this.FindOrderItemById (orderItemId: int) : OrderItem Task =
        Db.queryOne
            "SELECT * FROM order_items WHERE order_item_id = @OrderItemId"
            (Db.parameters {|OrderItemId = orderItemId|})
            (recordToOrderItem None)
            db

    ////
    // Relationships

    member private this.LoadItemsForOrders (orderIds: int[]): OrderItem list Task =
        Db.query
            """
            SELECT *
            FROM order_items
            WHERE order_id = ANY(@OrderIds)
            ORDER BY order_id asc, order_item_id asc
            """
            (Db.parameters {|OrderIds = orderIds|})
            (recordToOrderItem None)
            db

    member this.IncludeItems(orders: Order list) : Order list Task =
        task {
            let! items = this.LoadItemsForOrders [|for o in orders -> o.OrderId|]
            return
                query {
                    for o in orders do
                    groupJoin i in items on (o.OrderId = i.OrderId) into orderItems
                    select {o with OrderItems = orderItems |> List.ofSeq |> Some}
                } |> List.ofSeq
        }

    member this.IncludeItems(order: Order): Order Task =
        this.IncludeItems [order] |> Task.map (List.exactlyOne)

    member this.IncludeOrder (orderItem: OrderItem): OrderItem Task =
        this.FindOrderById(orderItem.OrderId)
        |> Task.map (fun x -> {orderItem with Order = Some x})

    ////
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
