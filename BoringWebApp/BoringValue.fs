namespace BoringWebApp

open System.Data
open System.ComponentModel.DataAnnotations
open System.ComponentModel.DataAnnotations.Schema

open BoringWebApp.DataRecordHelpers

[<CLIMutable>]
type BoringValue =
    {
        [<Key>]
        [<Column("id")>]
        Id: int

        [<Column("value")>]
        Value: string
    }
    static member FromDb (r: IDataRecord) =
        {Id = r?id; Value=r?value}
