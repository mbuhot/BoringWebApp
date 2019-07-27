namespace BoringWebApp
open System
open System.Data
open System.Data.Common
open System.Threading.Tasks
open FSharp.Control.Tasks.V2
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection

module DataRecordHelpers =
    let (?) (self: IDataRecord) (name: string) : 'a =
        match self.[name] with
        | :? DBNull -> Unchecked.defaultof<'a>
        | value -> value :?> 'a

module Db =
    let createConnection (configuration: IConfiguration) =
        let connection = new Npgsql.NpgsqlConnection(configuration.GetConnectionString("db"))
        connection.Open()
        connection :> DbConnection

    type Parameters = (String * obj) seq

    let parameters (props: 'p) : Parameters =
        props.GetType().GetProperties()
        |> Array.map (fun p -> "@" + p.Name, p.GetValue props) :> _

    let private bindParameters (parameters: Parameters) (command: DbCommand) =
        for (name, value) in parameters do
            let parameter = command.CreateParameter()
            parameter.ParameterName <- name
            parameter.Value <- if isNull value then box DBNull.Value else value
            command.Parameters.Add(parameter) |> ignore

    let rec private readAll (f: IDataRecord -> 'a) (acc: 'a list) (reader: DbDataReader) =
        task {
            match! reader.ReadAsync() with
            | true ->
                let acc = (f reader) :: acc
                return! readAll f acc reader
            | _ ->
                return List.rev acc
        }

    let query (sql: string) (parameters: Parameters) (f: IDataRecord -> 'a) (conn: DbConnection): 'a list Task =
        task {
            use command = conn.CreateCommand()
            command.CommandText <- sql
            bindParameters parameters command
            use! reader = command.ExecuteReaderAsync()
            return! readAll f [] reader
        }

    let queryOne (sql: string) (parameters: Parameters) (f: IDataRecord -> 'a) (conn: DbConnection): 'a Task =
        task {
            let! res = query sql parameters f conn
            return List.exactlyOne res
        }

    let execute (sql: string) (parameters: Parameters) (conn: DbConnection) =
        use command = conn.CreateCommand()
        command.CommandText <- sql
        bindParameters parameters command
        command.ExecuteNonQueryAsync()

    let updateOne (sql: string) (parameters: Parameters) (conn: DbConnection) =
        task {
            let! rowsUpdated = execute sql parameters conn
            match rowsUpdated with
            | 1 -> ()
            | _ -> failwith "Update failed"
        }

