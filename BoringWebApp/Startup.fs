namespace BoringWebApp

open System.Data.Common
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
//open Microsoft.AspNetCore.HttpsPolicy;
//open Microsoft.AspNetCore.Mvc
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting

open BoringWebApp.Values
open BoringWebApp.Orders


type Startup (configuration: IConfiguration) =
    member this.Configuration = configuration

    // This method gets called by the runtime. Use this method to add services to the container.
    member this.ConfigureServices(services: IServiceCollection) =
        // Add framework services.
        services
            .AddScoped<DbConnection>(fun _ -> Db.createConnection configuration)
            .AddTransient<ValueRepository>()
            .AddTransient<OrderRepository>()
            .AddTransient<ValuesRouteHelpers>()
            .AddTransient<OrdersRouteHelpers>()
            .AddAuthorization()
            .AddControllers()
        |> ignore

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

