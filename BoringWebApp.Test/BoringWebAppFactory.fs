namespace BoringWebApp.Test
open System.Data
open System.Data.Common
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Mvc.Testing
open Microsoft.Extensions.DependencyInjection

/// Customized WebApplicationFactory for testing.
/// A TestDbConnection is registered as a singleton, allowing tests to be isolated.
///
/// Usage:
/// module MyIntegrationTests
/// let factory = BoringWebApplicationFactory()
///
/// [<Fact>]
/// let ``List all widgets``() = task {
///   use txn = factory.DbConnection.BeginTransaction()
///   let client = factory.CreateClient()
///   client.GetJsonAsync("/widgets")
///   ...
/// }
type BoringWebApplicationFactory() =
    inherit WebApplicationFactory<BoringWebApp.Startup>()

    let dbConnection =
        let connString = "Host=localhost;Username=postgres;Password=password;Database=boring_web_app_test"
        let connection = new Npgsql.NpgsqlConnection(connString)
        connection.Open()
        connection :> DbConnection

    member this.DbConnection = dbConnection

    override this.ConfigureWebHost(hostBuilder: IWebHostBuilder) =
        hostBuilder.ConfigureServices(fun services ->
            services.AddSingleton<DbConnection>(new TestDbConnection(dbConnection))
            |> ignore
        )
        |> ignore


