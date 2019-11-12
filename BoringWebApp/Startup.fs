namespace BoringWebApp

open System.Data.Common
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
//open Microsoft.AspNetCore.HttpsPolicy;
//open Microsoft.AspNetCore.Mvc
open Microsoft.OpenApi.Models
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting

open System.IO
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
            .AddTransient<Persistence.OrderRepository>()
            .AddTransient<ValuesRouteHelpers>()
            .AddTransient<OrdersRouteHelpers>()
            .AddAuthorization()
            .AddControllers()
        |> ignore

        services
            .AddSwaggerGen(fun c ->
                c.SwaggerDoc("v1", new OpenApiInfo(Title = "Boring Web App", Version = "v1"))
                let filePath = Path.Combine(System.AppContext.BaseDirectory, "BoringWebApp.xml")
                c.IncludeXmlComments(filePath)
            )
        |> ignore

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    member this.Configure(app: IApplicationBuilder, env: IWebHostEnvironment) =
        if (env.IsDevelopment()) then
            app.UseDeveloperExceptionPage() |> ignore
        else
            app.UseHsts() |> ignore

        app
            .UseHttpsRedirection()
            .UseRouting()
            .UseAuthorization()
            .UseSwagger()
            .UseSwaggerUI(fun x ->
                x.SwaggerEndpoint("/swagger/v1/swagger.json", "Boring Web App V1")
            )
            .UseEndpoints(fun endpoints -> endpoints.MapControllers() |> ignore)
        |> ignore

