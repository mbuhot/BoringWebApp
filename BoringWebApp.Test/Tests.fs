module Tests

open System
open System.Data
open System.Threading.Tasks
open FSharp.Control.Tasks.V2
open Microsoft.Extensions.DependencyInjection

open Xunit

open BoringWebApp
open BoringWebApp.Controllers

type HttpClient = System.Net.Http.HttpClient
type WebApplicationFactory<'a when 'a: not struct> = Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<'a>

[<CLIMutable>]
type MyResponseType = {
    Id: int
}

module HttpClient =
    open Microsoft.AspNetCore.Components
    let postJsonAsync (path: string) (body: 'a) (client: HttpClient): Task<'b> =
        client.PostJsonAsync<'b>(path, body)

    let putJsonAsync (path: string) (body: 'a) (client: HttpClient): Task<'b> =
        client.PutJsonAsync<'b>(path, body)

    let getJsonAsync (path: string) (client: HttpClient): Task<'a> =
        client.GetJsonAsync<'a>(path)

    let getStringAsync (path: string) (client: HttpClient): Task<string> =
        client.GetStringAsync(path)

module Should =
    let equal (expected: 'a) (actual: 'a) =
        Assert.Equal<'a>(expected, actual)

    let beGreaterThan<'a when 'a :> IComparable<'a>> (expected: 'a) (actual: 'a) =
        Assert.True(actual.CompareTo(expected) > 0, sprintf "Expected %A to be greater than %A" actual expected)

module WebApplicationFactory =
    let service<'a> (factory: WebApplicationFactory<_>) : 'a =
        factory.Services.GetRequiredService(typeof<'a>) :?> 'a

    let client (factory: WebApplicationFactory<_>) : HttpClient =
        factory.CreateClient()

type TestTransaction(db: IDbConnection, il: IsolationLevel) =
    interface IDbTransaction with
        member this.Connection = db
        member this.IsolationLevel = il
        member this.Commit() = ()
        member this.Rollback() = raise <| NotSupportedException()

    interface IDisposable with
        member this.Dispose() = ()

type TestDbConnection(db: IDbConnection) =
    interface IDisposable with
        member this.Dispose() = ()

    interface IDbConnection with
        member this.ConnectionString
            with get() = db.ConnectionString and
                 set x = db.ConnectionString <- x

        member this.ConnectionTimeout = db.ConnectionTimeout
        member this.Database = db.Database
        member this.State = db.State
        member this.BeginTransaction() = (this :> IDbConnection).BeginTransaction IsolationLevel.ReadCommitted
        member this.BeginTransaction(il: IsolationLevel) = new TestTransaction(this, il) :> IDbTransaction
        member this.CreateCommand() = db.CreateCommand()
        member this.Open() = raise <| NotSupportedException()
        member this.Close() = raise <| NotSupportedException()
        member this.ChangeDatabase(_databaseName: string) = raise <| NotSupportedException()

let testConn =
    let connString = "Host=localhost;Username=postgres;Password=password;Database=boring_web_app_test"
    let connection = new Npgsql.NpgsqlConnection(connString)
    connection.Open()
    connection :> IDbConnection

let factory =
    (new WebApplicationFactory<BoringWebApp.Startup>())
        .WithWebHostBuilder(fun hostBuilder ->
            hostBuilder.ConfigureServices(fun services ->
                services.AddSingleton<IDbConnection>(new TestDbConnection(testConn))
                |> ignore
            )
            |> ignore
        )

type ValuesTest() =
    let txn = testConn.BeginTransaction()
    let valuesRoutes : ValuesRouteHelpers = factory |> WebApplicationFactory.service
    let client = factory |> WebApplicationFactory.client

    [<Fact>]
    let IndexValues() = task {
        let! _ = client |> HttpClient.postJsonAsync valuesRoutes.Create { Value = "value1" }
        let! _ = client |> HttpClient.postJsonAsync valuesRoutes.Create { Value = "value2" }
        let! (response: BoringValue[]) = client |> HttpClient.getJsonAsync valuesRoutes.Index
        response |> Array.map (fun x -> x.Value) |> Should.equal [|"value1"; "value2"|]
    }

    [<Fact>]
    let ShowValue() = task {
        let! response = client |> HttpClient.postJsonAsync valuesRoutes.Create { Value = "Hello" }
        let! (response: BoringValue) = client |> HttpClient.getJsonAsync (valuesRoutes.Show response.Id)
        response.Value |> Should.equal "Hello"
    }

    [<Fact>]
    let CreateValue() = task {
        let! response = client |> HttpClient.postJsonAsync valuesRoutes.Create { Value = "Hello" }
        response.Id |> Should.beGreaterThan 0
    }

    [<Fact>]
    let UpdateValue() = task {
        let! response = client |> HttpClient.postJsonAsync valuesRoutes.Create { Value = "Hello" }
        let! response = client |> HttpClient.postJsonAsync (valuesRoutes.Update response.Id) { Value = "Hello2" }
        response.Id |> Should.beGreaterThan 0
    }

    interface IDisposable with member this.Dispose() = txn.Dispose()
