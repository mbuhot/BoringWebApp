open System.Data

// Learn more about F# at http://fsharp.org

let openConnection() =
    let connection = new Npgsql.NpgsqlConnection("Host=localhost;Username=postgres;Password=password")
    connection.Open()
    connection

let exec sql (db: IDbConnection) =
    let command = db.CreateCommand()
    command.CommandText <- sql
    command.ExecuteNonQuery() |> ignore

let dropDatabase() =
    use connection = openConnection()
    connection |> exec "DROP DATABASE boring_web_app_test"
    printfn "Dropped database boring_web_app_test"

let createDatabase() =
    use connection = openConnection()
    connection |> exec "CREATE DATABASE boring_web_app_test"
    printfn "Created database boring_web_app_test"

let migrateDatabase() =
    use connection = openConnection()
    connection.ChangeDatabase("boring_web_app_test")
    connection |> exec  """
        CREATE TABLE IF NOT EXISTS boring_values (
            id serial primary key,
            value text not null
        );
        """

    connection |> exec """
        CREATE TABLE IF NOT EXISTS orders (
            order_id serial primary key,
            created_at timestamptz not null,
            customer text not null,
            discount_code text default null,
            status text not null
        );
        """

    connection |> exec """
        CREATE TABLE IF NOT EXISTS order_items (
            order_item_id serial primary key,
            order_id integer references orders(order_id),
            product text not null,
            unit_price decimal not null,
            quantity integer not null
        );
        """

    printfn "Migrated database boring_web_app_test"

[<EntryPoint>]
let main argv =
    match argv with
    | [|"db.drop"|] -> dropDatabase()
    | [|"db.create"|] -> createDatabase()
    | [|"db.migrate"|] -> migrateDatabase()
    | _otherwise -> printfn "I don't understand %A" argv
    0 // return an integer exit code
