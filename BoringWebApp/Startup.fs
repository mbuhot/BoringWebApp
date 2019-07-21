namespace BoringWebApp

open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.HttpsPolicy;
open Microsoft.AspNetCore.Mvc
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting

open BoringWebApp.Controllers

type Startup private () =
    new (configuration: IConfiguration) as this =
        Startup() then
        this.Configuration <- configuration

    member val Configuration : IConfiguration = null with get, set

    // This method gets called by the runtime. Use this method to add services to the container.
    member this.ConfigureServices(services: IServiceCollection) =
        // Add framework services.
        services.AddControllers() |> ignore
        services.AddTransient<ValuesRouteHelpers>() |> ignore

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

