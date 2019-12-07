namespace BoringWebApp.Orders.Persistence

open System.Data
open System.Data.Common
open System.Threading.Tasks
open BoringWebApp
open BoringWebApp.Orders
open BoringWebApp.Db.Operators
open FSharp.Control.Tasks.V2

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

    let recordToOrderItem (r: IDataRecord) : OrderItem =
        {
            OrderItemId = r?order_item_id
            OrderId = r?order_id
            Product = r?product
            UnitPrice = r?unit_price
            Quantity = r?quantity
            Order = None
        }

/// Defines functions to load related data and attach to Order domain types
module private Relations =
    let findItemsForOrderIds (orderIds: int[]) : OrderItem Query =
        Query.Create(
            """
            SELECT *
            FROM order_items
            WHERE order_id = ANY(@OrderIds)
            ORDER BY order_id asc, order_item_id asc
            """,
            Bindings.recordToOrderItem,
            Db.parameters {|OrderIds = orderIds|}
        )

    let private orderItems (db: DbConnection) (orders: Order list) : Order list Task =
        let combineItemsWithOrders (items: OrderItem list) =
            query {
                for o in orders do
                groupJoin i in items on (o.OrderId = i.OrderId) into orderItems
                select {o with OrderItems = orderItems |> List.ofSeq |> Some}
            } |> List.ofSeq

        let query = findItemsForOrderIds [|for o in orders -> o.OrderId|]
        Db.query query.Sql query.Parameters query.Mapper db
        |> Task.map combineItemsWithOrders

    let private includeOrderRelation (db: DbConnection) (orders: Order list) = function
        | IncludeItems _ -> orderItems db orders
        | IncludeCustomer _ -> orders |> Task.FromResult

    let includeOrderRelations (includes: OrderIncludes list) (db: DbConnection) (orders: Order list) : Order list Task =
        Task.fold (includeOrderRelation db) orders includes


/// Defines the DB queries for loading Order Domain types by ids or other attributes
module OrderQuery =

    let private orderFilter = function
        | WithId p -> "(order_id = @OrderId)", (Db.parameters p)
        | CreatedBetween p -> "((created_at >= @MinCreatedAt) AND (created_at <= @MaxCreatedAt))", (Db.parameters p)
        | WithStatus p -> "(status = @Status)", (Db.parameters p)
        | WithCustomer p -> "(customer = @Customer)", (Db.parameters p)

    let private buildWhere (filters: OrderFilter list) =
        Db.buildWhere orderFilter filters

    let private orderSort =  function
        | SortByCustomer -> "customer"
        | SortById -> "id"
        | SortByStatus -> "status"
        | SortByCreatedAt -> "created_at"

    let private buildOrderBy (sorts: OrderSort list) =
        Db.buildOrderBy orderSort sorts

    let toSql (query: OrderQuery) =
        let whereClause, parameters = buildWhere query.Filters
        let orderByClause = buildOrderBy query.Sort
        Query.Create(
            sprintf "SELECT * FROM orders %s %s" whereClause orderByClause,
            Bindings.recordToOrder,
            parameters,
            Relations.includeOrderRelations query.Includes
        )


/// Exposes a facade that can be injected into a Controller
type OrderRepository(db: DbConnection) =

    member this.Query(query: OrderQuery) =
        query
        |> OrderQuery.toSql
        |> Query.query db

    member this.QueryOne(query: OrderQuery) =
        query
        |> OrderQuery.toSql
        |> Query.queryOne db

    member this.Save (events: OrderEvent list) =
        task {
            use! txn = db.BeginTransactionAsync()
            for e in events do
                match e with
                | OrderCreated x ->
                    do! Db.insert x ignore db

                | ItemAdded x ->
                    do! Db.insert x ignore db

                | ItemQuantityChanged x ->
                    do! Db.updateByPrimaryKey x db

                | ItemRemoved x ->
                    do! Db.deleteByPrimaryKey x db

                | OrderSubmitted x ->
                    do! Db.updateByPrimaryKey x db

            do! txn.CommitAsync()
        }

    member this.SaveOrder (event: OrderCreated) : int Task =
        Db.insert event (fun x -> x?order_id) db

    member this.SaveOrderItem (event: ItemAdded) : int Task =
        Db.insert event (fun x -> x?order_item_id) db

