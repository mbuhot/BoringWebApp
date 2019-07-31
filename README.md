# Boring Web App

Using ASP.NET Core in 'the usual way' from F#

## What is interesting about this Repo?

This repo demonstrates a functional approach to using SQL that:

 - Allows any SQL to be used
 - Allows mapping SQL records to plain-old F# types explicitly
 - Requires no mutable fields or data binding
 - Does not use change tracking
 - Does not rely on naming conventions
 - Does not rely on additional packages beyond the Npgsql driver
 - Allows tests to run against the Database while remaining isolated


### Queries are SQL

```fsharp
let findItemsForOrderIds (orderIds: int[]) (db: DbConnection): OrderItem list Task =
    Db.query
        """
        SELECT *
        FROM order_items
        WHERE order_id = ANY(@OrderIds)
        ORDER BY order_id asc, order_item_id asc
        """
        (Db.parameters {|OrderIds = orderIds|})
        Bindings.recordToOrderItem
        db
```

### Updates are Event Oriented

A record type describes the semantic change that needs to be made, and which table/columns will be updated:

```fsharp
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
```

The `Db` module can then apply the changes to the database:

```fsharp
type OrderRepository(db: DbConnection) =
    member this.UpdateQuantity (event: ItemQuantityChanged) =
        db |> Db.updateByPrimaryKey event
```

### Tests are Isolated

```fsharp
type OrdersTests() =
    // Create a factory once per test class
    static let factory = new BoringWebApplicationFactory()

    // Start a test transaction for each tests
    let txn = factory.DbConnection.BeginTransaction()

    // Get the HTTP client and route helpers
    let routes = factory.Services.GetRequiredService<OrdersRouteHelpers>()
    let client = factory.CreateClient()

    // Rollback the test transaction between tests
    interface IDisposable with member this.Dispose() = txn.Dispose()

    [<Fact>]
    member this.``Index lists all created Orders``() = task {
        let! _ = client |> HttpClient.postJsonAsync routes.Create { Customer = "Henry" }
        let! _ = client |> HttpClient.postJsonAsync routes.Create { Customer = "Percy" }
        let! _ = client |> HttpClient.postJsonAsync routes.Create { Customer = "James" }
        let! (response: OrderResponse[]) = client |> HttpClient.getJsonAsync routes.Index
        response |> Array.map (fun x -> x.Customer) |> Should.equal [|"Henry"; "Percy"; "James"|]
    }
```

## Get Started

Install the latest .NET core 3.0 preview (3.0.100-preview7-012821)

```
git clone https://github.com/mbuhot/BoringWebApp.git
cd BoringWebApp
dotnet build
```

## Create and Migrate Database

```
docker-compose up -d
dotnet run -p BoringWebApp.Up/BoringWebApp.Up.fsproj db.create
dotnet run -p BoringWebApp.Up/BoringWebApp.Up.fsproj db.migrate
```

## Run Tests

```
dotnet test
```

## Run Server

```
dotnet run -p BoringWebApp/BoringWebApp.fsproj
```

## View Swagger UI

```
open http://localhost:5000/swagger/index.html
```
