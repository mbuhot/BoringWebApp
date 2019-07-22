namespace BoringWebApp

open System.Data
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
//open Microsoft.AspNetCore.HttpsPolicy;
//open Microsoft.AspNetCore.Mvc
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting

open BoringWebApp.Controllers

type Startup (configuration: IConfiguration)=
    let addScopedDbConnection (services: IServiceCollection) =
        services.AddScoped<IDbConnection>(fun _serviceProvider ->
            let connection = new Npgsql.NpgsqlConnection(configuration.GetConnectionString("db"))
            connection.Open()
            connection :> IDbConnection
        ) |> ignore

    member this.Configuration = configuration

    // This method gets called by the runtime. Use this method to add services to the container.
    member this.ConfigureServices(services: IServiceCollection) =
        // Add framework services.
        services |> addScopedDbConnection
        ignore <| services.AddTransient<ValueRepository>()
        ignore <| services.AddTransient<ValuesRouteHelpers>()
        ignore <| services.AddAuthorization()
        ignore <| services.AddControllers()

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    member this.Configure(app: IApplicationBuilder, env: IWebHostEnvironment) =
        if (env.IsDevelopment()) then
            ignore <| app.UseDeveloperExceptionPage()
        else
            ignore <| app.UseHsts()

        ignore <| app.UseHttpsRedirection()
        ignore <| app.UseRouting()
        ignore <| app.UseAuthorization()
        ignore <| app.UseEndpoints(fun endpoints -> endpoints.MapControllers() |> ignore)

