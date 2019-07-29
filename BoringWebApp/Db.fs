namespace BoringWebApp
open System
open System.ComponentModel.DataAnnotations
open System.ComponentModel.DataAnnotations.Schema
open System.Data
open System.Data.Common
open System.Reflection
open System.Threading.Tasks
open FSharp.Control.Tasks.V2
open Microsoft.Extensions.Configuration

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

    let private getTypeAttribute<'attr>(o: obj) =
        match o.GetType().GetCustomAttributes(typeof<'attr>, false) with
        | [|attr|] -> attr :?> 'attr
        | _ -> failwithf "Missing attribute %s on object %s" (typeof<'attr>.Name) (o.GetType().Name)

    let private getPropertyAttribute<'attr when 'attr :> Attribute>(p: PropertyInfo) =
        p.GetCustomAttributes<'attr>() |> Seq.head

    let private table (o: obj) =
        o
        |> getTypeAttribute<TableAttribute>
        |> (fun table -> table.Name)

    let private column (p: PropertyInfo) =
        p
        |> getPropertyAttribute<ColumnAttribute>
        |> (fun col -> col.Name)

    let private  join (sep: string) (strings: string seq) =
        String.Join(sep, strings)

    /// Convenience for inserting into a table using a parameter record annotated with [<Table>] and [<Column>] attributes
    /// All columns are returned, allowing any database generated values to be mapped back
    let insert (paramRecord: 'p) (mapRecord: IDataRecord -> 'a) (conn: DbConnection) =
        let columnNames = paramRecord.GetType().GetProperties() |> Array.map column |> join ", "
        let paramPairs = parameters paramRecord
        let paramNames = Seq.map fst paramPairs |> join ", "

        let sql =
            sprintf
                "INSERT INTO %s (%s) VALUES (%s) RETURNING *"
                (table paramRecord)
                columnNames
                paramNames

        queryOne sql paramPairs mapRecord conn

    /// Convenience for updating a single row using a parameter record annotated with [<Table>] [<Key>] and [<Column>] attributes
    let updateByPrimaryKey (paramRecord: 'p) (conn: DbConnection) =
        let (keys, others) =
            paramRecord
                .GetType()
                .GetProperties()
            |> Array.partition (fun p -> p.GetCustomAttribute<KeyAttribute>() |> isNull |> not)

        let key = keys |> Seq.head

        let columnAssignments =
            others
            |> Array.map (fun p -> sprintf "%s = @%s" (column p) p.Name)
            |> join ", "

        let sql =
            sprintf
                "UPDATE %s SET %s WHERE (%s = @%s)"
                (table paramRecord)
                columnAssignments
                (column key)
                key.Name

        updateOne sql (parameters paramRecord) conn

    /// Convenience for deleting a single row using a parameter record annotated with [<Table>] [<Key>] and [<Column>] attributes
    let deleteByPrimaryKey (paramRecord: 'p) (conn: DbConnection) =
        let key =
            paramRecord
                .GetType()
                .GetProperties()
            |> Array.find (fun p -> p.GetCustomAttribute<KeyAttribute>() |> isNull |> not)

        let sql =
            sprintf
                "DELETE FROM %s WHERE (%s = @%s)"
                (table paramRecord)
                (column key)
                key.Name

        updateOne sql (parameters paramRecord) conn




