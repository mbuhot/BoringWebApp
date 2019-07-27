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

    let queryOrders (sql: string) (parameters: Db.Parameters) =
        Db.query
            ("""
            SELECT order_id, created_at, customer, discount_code, status
            FROM orders
            """ + sql)
            parameters
            recordToOrder
            db

    let queryOrderItems (sql: string) (parameters: Db.Parameters) (order: Order option) =
        Db.query
            ("""
            SELECT order_item_id, order_id, product, unit_price, quantity
            FROM order_items
            """ + sql)
            parameters
            (recordToOrderItem order)
            db


    member this.AllOrders() : Order list Task =
        queryOrders "" []

    member this.AllOrdersWithItems() =
        task {
            let! orders = this.AllOrders()
            let! items = [| for o in orders -> o.OrderId |] |> this.LoadItemsForOrders
            return
                query {
                    for o in orders do
                    groupJoin i in items on (o.OrderId = i.OrderId) into group
                    select {o with OrderItems = Some (group |> List.ofSeq)}
                }
                |> Seq.toList
        }

    member this.FindOrderById(orderId: int) : Order Task =
        task {
            let! orders = queryOrders "WHERE order_id = @OrderId" (Db.parameters {|OrderId = orderId|})
            return orders |> List.exactlyOne
        }

    member this.LoadItemsForOrders (orderIds: int[]): OrderItem list Task =
        queryOrderItems
            """
            WHERE order_id = ANY(@OrderIds)
            ORDER BY order_id asc, order_item_id asc
            """
            (Db.parameters {|OrderIds = orderIds|})
            None

    member this.LoadItems(order: Order): Order Task =
        task {
            let! items = this.LoadItemsForOrders([|order.OrderId|])
            let items = items |> List.map (fun x -> {x with Order = (Some order)})
            return {order with OrderItems = Some items}
        }

    member this.LoadItemWithOrder (orderId: int) (orderItemId: int) : OrderItem Task =
        task {
            let! order = this.FindOrderById orderId
            let! items =
                queryOrderItems
                    "WHERE order_item_id = @OrderItemId"
                    (Db.parameters {|OrderId=orderId; OrderItemId=orderItemId|})
                    (Some order)

            return List.exactlyOne items
        }



    ////
    // Mutations

    member this.Insert(event: OrderCreated) : int Task =
        Db.queryOne """
            INSERT INTO orders (created_at, customer, discount_code, status)
            VALUES (@CreatedAt, @Customer, @DiscountCode, @Status)
            RETURNING order_id
            """
            (Db.parameters event.Order)
            (fun r -> r?order_id)
            db

    member this.AddItem(event: ItemAdded) : int Task =
        Db.queryOne """
            INSERT INTO order_items (order_id, product, unit_price, quantity)
            VALUES (@OrderId, @Product, @UnitPrice, @Quantity)
            RETURNING order_item_id
            """
            (Db.parameters event)
            (fun r -> r?order_item_id)
            db

    member this.UpdateQuantity(event: ItemQuantityChanged) =
        Db.updateOne
            """
            UPDATE order_items
            SET quantity = @Quantity
            WHERE (order_item_id = @OrderItemId) AND (product = @Product)
            """
            (Db.parameters event)
            db

    member this.UpdateStatus(event: OrderSubmitted) =
        Db.updateOne
            """
            UPDATE orders
            SET status = @Status
            WHERE (order_id = @OrderId)
            """
            (Db.parameters event)
            db

    member this.DeleteItem(event: ItemRemoved) =
        Db.updateOne
            """
            DELETE FROM order_items
            WHERE (order_item_id = @OrderItemId)
            """
            (Db.parameters event)
            db
