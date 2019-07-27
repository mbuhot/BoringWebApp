namespace BoringWebApp.Values

open System.Data
open System.Data.Common
open System.Threading.Tasks
open BoringWebApp
open BoringWebApp.DataRecordHelpers

type ValueRepository(db: DbConnection) =
    let fromDb (r: IDataRecord) : BoringValue =
        {Id = r?id; Value = r?value}

    member this.All() : BoringValue list Task=
        db |> Db.query "SELECT id, value FROM boring_values" [] fromDb

    member this.FindById(id: int) : BoringValue Task =
        let parameters = Db.parameters {|Id = id|}
        db |> Db.queryOne "SELECT id, value FROM boring_values WHERE id = @Id" parameters fromDb

    member this.FindByIdForUpdate(id: int) : BoringValue Task =
        let parameters = Db.parameters {|Id = id|}
        db |> Db.queryOne "SELECT id, value FROM boring_values WHERE id = @Id FOR UPDATE" parameters fromDb

    member this.Insert(value: BoringValue) : BoringValue Task =
        let parameters = Db.parameters {|Value = value.Value|}
        db |> Db.queryOne "INSERT INTO boring_values (value) VALUES (@Value) RETURNING id, value" parameters fromDb

    member this.Update(value: BoringValue) =
        let parameters = Db.parameters value
        db |> Db.execute "UPDATE boring_values SET value = @Value WHERE id = @Id" parameters

    member this.Delete(id: int) =
        let parameters = Db.parameters {|Id = id|}
        let sql = "DELETE FROM boring_values WHERE id = @Id"
        db |> Db.execute sql parameters

    member this.BeginTransaction() = db.BeginTransactionAsync()
