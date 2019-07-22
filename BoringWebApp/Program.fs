namespace BoringWebApp

//open Microsoft.AspNetCore
open Microsoft.AspNetCore.Hosting
//open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Hosting
//open Microsoft.Extensions.Logging

module Program =
    let CreateHostBuilder (args: string[]) : IHostBuilder =
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(fun webBuilder -> webBuilder.UseStartup<Startup>() |> ignore)

    [<EntryPoint>]
    let main args =
        CreateHostBuilder(args).Build().Run()
        0
