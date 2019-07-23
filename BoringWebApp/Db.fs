namespace BoringWebApp
open System
open System.Data
open System.Data.Common
open System.Threading.Tasks
open FSharp.Control.Tasks.V2

module DataRecordHelpers =
    let (?) (self: IDataRecord) (name: string) : 'a =
        self.[name] :?> 'a

module Db =
    let private bindParameters (parameters: (String * obj) seq) (command: DbCommand) =
        for (name, value) in parameters do
            let parameter = command.CreateParameter()
            parameter.ParameterName <- name
            parameter.Value <- value
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

    let query (sql: string) (parameters: (string * obj) seq) (f: IDataRecord -> 'a) (conn: DbConnection): 'a list Task =
        task {
            use command = conn.CreateCommand()
            command.CommandText <- sql
            bindParameters parameters command
            use! reader = command.ExecuteReaderAsync()
            return! readAll f [] reader
        }

    let queryOne (sql: string) (parameters: (string * obj) seq) (f: IDataRecord -> 'a) (conn: DbConnection): 'a Task =
        task {
            let! res = query sql parameters f conn
            return List.exactlyOne res
        }

    let execute (sql: string) (parameters: (string * obj) seq) (conn: DbConnection) =
        use command = conn.CreateCommand()
        command.CommandText <- sql
        bindParameters parameters command
        command.ExecuteNonQueryAsync()

    let parameters (props: 'p) =
        props.GetType().GetProperties()
        |> Array.map (fun p -> "@" + p.Name, p.GetValue props)

