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

/// <summary>
/// A functional SQL mapper for F#
/// </summary>
module Db =

    /// <summary>
    /// Provides helper operators for mapping from IDataRecord to application types
    /// </summary>
    module Operators =

        /// <summary>
        /// Retrieves a field from IDataRecord by name, converting to the expected type
        /// </summary>
        /// <remarks>
        /// If the value from the Database is null, a default value is used
        /// </remarks>
        let (?) (self: IDataRecord) (name: string): 'a =
            match self.[name] with
            | :? DBNull -> Unchecked.defaultof<'a>
            | value -> value :?> 'a

    /// <summary>
    /// Build and Open a connection string from the ConnectionStrings:db configuration
    /// </summary>
    let createConnection (configuration: IConfiguration) =
        let connection = new Npgsql.NpgsqlConnection(configuration.GetConnectionString("db"))
        connection.Open()
        connection :> DbConnection

    /// <summary>
    /// A parameter list to use with a Database statement
    /// </summary>
    type Parameters = (String * obj) seq

    let private properties (paramRecord: 'p) =
        paramRecord.GetType().GetProperties()

    /// <summary>
    /// Converts the properties of a record to a Parameters
    /// </summary>
    /// <remarks>
    /// The name of each parameter is the same as the property name, prefixed with '@'
    /// </remarks>
    let parameters (paramRecord: 'p): Parameters =
        paramRecord
        |> properties
        |> Array.map (fun p -> "@" + p.Name, p.GetValue paramRecord) :> _

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

    /// <summary>
    /// Execute a query given a SQL string, parameters and a function to map results
    /// </summary>
    /// <returns>
    /// A list of mapped result records as an asynchronous Task
    /// </returns>
    let query (sql: string) (parameters: Parameters) (mapRecord: IDataRecord -> 'a) (conn: DbConnection): 'a list Task =
        task {
            use command = conn.CreateCommand()
            command.CommandText <- sql
            bindParameters parameters command
            use! reader = command.ExecuteReaderAsync()
            return! readAll mapRecord [] reader
        }

    /// <summary>
    /// Execute a query that is expected to produce exactly one result row.
    /// </summary>
    /// <returns>
    /// The single result record of the query
    /// </returns>
    let queryOne (sql: string) (parameters: Parameters) (mapRecord: IDataRecord -> 'a) (conn: DbConnection): 'a Task =
        query sql parameters mapRecord conn
        |> Task.map List.exactlyOne


    /// <summary>
    /// Execute a SQL statement with given parameters
    /// </summary>
    /// <returns>
    /// The number of rows affected as an async Task
    /// </returns>
    let execute (sql: string) (parameters: Parameters) (conn: DbConnection) =
        use command = conn.CreateCommand()
        command.CommandText <- sql
        bindParameters parameters command
        command.ExecuteNonQueryAsync()

    /// <summary>
    /// Execute a SQL statement that is expected to update a single row
    /// </summary>
    /// <remarks>
    /// An exception will be raised if the number of rows affected was not equal to 1
    /// </remarks>
    let updateOne (sql: string) (parameters: Parameters) (conn: DbConnection) =
        task {
            let! rowsUpdated = execute sql parameters conn
            match rowsUpdated with
            | 1 -> ()
            | n -> failwithf "Expected 1 row to be affected, got %d" n
        }

    let private getTypeAttribute<'attr when 'attr :> Attribute> (o: obj) =
        match o.GetType().GetCustomAttribute(typeof<'attr>, false) with
        | null -> failwithf "Missing attribute %s on object %s" (typeof<'attr>.Name) (o.GetType().Name)
        | attr -> attr :?> 'attr

    let private table (o: obj) =
        let table = o |> getTypeAttribute<TableAttribute>
        table.Name

    let private column (p: PropertyInfo) =
        p.GetCustomAttribute<ColumnAttribute>().Name

    /// <summary>
    /// Convenience for inserting into a table using a parameter record annotated with <see cref="Table"/> and <see cref="Column"/> attributes
    /// </summary>
    /// <remarks>
    /// All columns are available for mapping back to an application type
    /// </remarks>
    /// <returns>
    /// The mapped result record
    /// </returns>
    let insert (paramRecord: 'p) (mapRecord: IDataRecord -> 'a) (conn: DbConnection) =
        let columnNames = paramRecord |> properties |> Array.map column |> String.concat ", "
        let paramPairs = parameters paramRecord
        let paramNames = Seq.map fst paramPairs |> String.concat ", "

        let sql =
            sprintf
                "INSERT INTO %s (%s) VALUES (%s) RETURNING *"
                (table paramRecord)
                columnNames
                paramNames

        queryOne sql paramPairs mapRecord conn

    let private partitionProperties (paramRecord: 'p) =
        let isKey (p: PropertyInfo) = p.GetCustomAttribute<KeyAttribute>() |> isNull |> not
        let (keys, others) = paramRecord |> properties |> Array.partition isKey
        (Array.head keys, others)

    /// <summary>
    /// Convenience for updating a single row using a parameter record annotated with <see cref="Table"/>, <see cref="Key"/> and <see cref="Column"/> attributes
    /// </summary>
    let updateByPrimaryKey (paramRecord: 'p) (conn: DbConnection) =
        let keyProperty, nonKeyProperties = partitionProperties paramRecord

        let columnAssignments =
            nonKeyProperties
            |> Array.map (fun p -> sprintf "%s = @%s" (column p) p.Name)
            |> String.concat ", "

        let sql =
            sprintf
                "UPDATE %s SET %s WHERE (%s = @%s)"
                (table paramRecord)
                columnAssignments
                (column keyProperty)
                keyProperty.Name

        updateOne sql (parameters paramRecord) conn

    /// <summary>
    /// Convenience for deleting a single row using a parameter record annotated with <see cref="Table"/>, <see cref="Key"/> and <see cref="Column"/> attributes
    /// </summary>
    let deleteByPrimaryKey (paramRecord: 'p) (conn: DbConnection) =
        let key, _others = partitionProperties paramRecord
        let sql =
            sprintf
                "DELETE FROM %s WHERE (%s = @%s)"
                (table paramRecord)
                (column key)
                key.Name

        updateOne sql (parameters paramRecord) conn


    /// <summary>
    /// Helper for dynamically building a WHERE clause from a list of Filters
    /// </summary>
    let buildWhere (mkFilter: 'Filter -> (string * Parameters)) (filters: 'Filter list): string * Parameters =
        let sqlList, paramList =
            match filters with
            | [] -> [ "true", [] :> Parameters ]
            | _ -> filters |> List.map mkFilter
            |> List.unzip

        let sql = "WHERE " + System.String.Join(" AND ", sqlList)
        let parameters = Seq.concat paramList
        sql, parameters

    /// <summary>
    /// Helper for dynamically building an ORDER BY clause from a list of Sort values
    /// </summary>
    let buildOrderBy (mkSort: 'Sort -> string) (sorts: 'Sort list): string =
        sorts
        |> List.map mkSort
        |> function
            | [] -> ""
            | columns -> "ORDER BY " + System.String.Join(", ", columns)


type Query<'a> =
        {
            Sql: string
            Parameters: Db.Parameters
            Mapper: (IDataRecord -> 'a)
            Include: DbConnection -> 'a list -> 'a list Task
        }
        static member Create(sql: string, mapper: (IDataRecord -> 'a), ?parameters: Db.Parameters, ?including: DbConnection -> 'a list -> 'a list Task) =
            {
                Sql = sql
                Mapper = mapper
                Parameters = defaultArg parameters Seq.empty
                Include = defaultArg including (fun db results -> Task.FromResult results)
            }

 module Query =
    let including (f: DbConnection -> 'a list -> 'a list Task) (q: Query<'a>) =
        { q with
            Include = (fun db xs -> q.Include db xs |> Task.bind (f db))
        }

    let map (f: 'a -> 'b) (query: Query<'a>) =
        Query.Create(query.Sql, query.Mapper >> f, query.Parameters)

    let query (db: DbConnection) (query: 'a Query) : 'a list Task =
        Db.query query.Sql query.Parameters query.Mapper db
        |> Task.bind (query.Include db)

    let queryOne (db: DbConnection) (query: 'a Query) : 'a Task =
        Db.query query.Sql query.Parameters query.Mapper db
        |> Task.bind (query.Include db)
        |> Task.map (List.exactlyOne)
