namespace BoringWebApp
open System
open System.ComponentModel.DataAnnotations
open System.ComponentModel.DataAnnotations.Schema
open System.Data

module DataRecordHelpers =
    let (?) (self: IDataRecord) (name: string) : 'a =
        self.[name] :?> 'a

module Db =
    let bindParameters (parameters: 'p) (command: IDbCommand) =
        for p in parameters.GetType().GetProperties() do
            let parameter = command.CreateParameter()
            parameter.Value <- p.GetValue(parameters)
            parameter.ParameterName <- "@" + p.Name
            command.Parameters.Add(parameter) |> ignore

    let query (sql: string) (parameters: 'parameters) (map: IDataRecord -> 'a) (conn: IDbConnection): 'a List =
        use command = conn.CreateCommand()
        command.CommandText <- sql
        if not (isNull (parameters :> obj)) then
            command |> bindParameters parameters
        use reader = command.ExecuteReader()
        [ while reader.Read() do
            yield map reader
        ]

    let queryOne sql parameters map conn =
        query sql parameters map conn |> List.head

    let execute (sql: string) (parameters: 'p) (conn: IDbConnection) =
        use command = conn.CreateCommand()
        command.CommandText <- sql
        if not <| isNull (parameters :> obj) then bindParameters parameters command
        command.ExecuteNonQuery()

    let columnAssignments (parameters: 'p) : string =
        seq {
            for p in parameters.GetType().GetProperties() do
                let column = p.GetCustomAttributes(typeof<ColumnAttribute>, true) |> Array.head :?> ColumnAttribute
                let isPrimaryKey = p.GetCustomAttributes(typeof<KeyAttribute>, true) |> Array.isEmpty
                if not isPrimaryKey then
                    yield column.Name + " = @" + p.Name
            }
        |> (fun x -> String.Join(", ", x))


