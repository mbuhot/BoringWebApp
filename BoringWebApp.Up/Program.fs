// Learn more about F# at http://fsharp.org

open System
open System.Data

let openConnection() =
    let connection = new Npgsql.NpgsqlConnection("Host=localhost;Username=postgres;Password=password")
    connection.Open()
    connection

let dropDatabase() =
    use connection = openConnection()
    let command = connection.CreateCommand()
    command.CommandText <- "DROP DATABASE boring_web_app_test"
    command.ExecuteNonQuery() |> ignore
    printfn "Dropped database boring_web_app_test"

let createDatabase() =
    use connection = openConnection()
    let command = connection.CreateCommand()
    command.CommandText <- "CREATE DATABASE boring_web_app_test"
    command.ExecuteNonQuery() |> ignore
    printfn "Created database boring_web_app_test"

let migrateDatabase() =
    use connection = openConnection()
    connection.ChangeDatabase("boring_web_app_test")
    let command = connection.CreateCommand()
    command.CommandText <-
        """
        CREATE TABLE boring_values (
            id serial primary key,
            value text not null
        );
        """
    command.ExecuteNonQuery() |> ignore
    printfn "Migrated database boring_web_app_test"

[<EntryPoint>]
let main argv =
    match argv with
    | [|"db.drop"|] -> dropDatabase()
    | [|"db.create"|] -> createDatabase()
    | [|"db.migrate"|] -> migrateDatabase()
    | otherwise -> printfn "I don't understand %A" argv
    0 // return an integer exit code
