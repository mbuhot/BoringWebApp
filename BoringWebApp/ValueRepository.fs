namespace BoringWebApp
open System.Data


type ValueRepository(db: IDbConnection) =
    member this.All() : BoringValue list =
        db |> Db.query "SELECT id, value FROM boring_values" None BoringValue.FromDb

    member this.FindById(id: int) : BoringValue =
        let parameters = {|Id = id|}
        db |> Db.queryOne "SELECT id, value FROM boring_values WHERE id=@Id" parameters BoringValue.FromDb

    member this.FindByIdForUpdate(id: int) : BoringValue =
        let parameters = {|Id = id|}
        db |> Db.queryOne "SELECT id, value FROM boring_values WHERE id=@Id FOR UPDATE" parameters BoringValue.FromDb

    member this.Insert(value: BoringValue) : BoringValue =
        let parameters = {|Value = value.Value|}
        db |> Db.queryOne "INSERT INTO boring_values (value) VALUES (@Value) RETURNING id, value" parameters BoringValue.FromDb

    member this.Update(parameters: 'p) =
        let sql = "UPDATE boring_values SET " + (Db.columnAssignments parameters) + " WHERE id = @Id"
        db |> Db.execute sql parameters

    member this.Delete(id: int) =
        let parameters = {|Id=id|}
        let sql = "DELETE FROM boring_values WHERE id = @Id"
        db |> Db.execute sql parameters

    member this.BeginTransaction() = db.BeginTransaction()

