module CnWhiteList.App

open System
open System.IO
open System.Net.Http
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Microsoft.Extensions.DependencyInjection
open Giraffe


// ---------------------------------
// Web app
// ---------------------------------

let generateHandler (ctx: HttpContext) =
    let logger = ctx.GetLogger("generator")
    let http = ctx.GetService<IHttpClientFactory>()
    Generator.generateWhiteList logger http
    |> ctx.WriteTextAsync

let webApp =
    choose [
        GET >=> choose [ route "/" >=> publicResponseCaching (60 * 5) None >=> handleContext generateHandler ]
        setStatusCode 404 >=> text "Not Found"
    ]

// ---------------------------------
// Error handler
// ---------------------------------

let errorHandler (ex: Exception) (logger: ILogger) =
    logger.LogError(ex, "An unhandled exception has occurred while executing the request.")
    clearResponse
    >=> setStatusCode 500
    >=> text ex.Message

// ---------------------------------
// Config and Main
// ---------------------------------


let configureApp (app: IApplicationBuilder) =
    app.UseResponseCaching() |> ignore
    let env =
        app.ApplicationServices.GetService<IWebHostEnvironment>()

    (match env.EnvironmentName with
     | "Development" -> app.UseDeveloperExceptionPage()
     | _ -> app.UseGiraffeErrorHandler(errorHandler)).UseGiraffe(webApp)

let configureServices (services: IServiceCollection) =
    services
        .AddGiraffe()
        .AddMemoryCache()
        .AddHttpClient()
        .AddResponseCaching()
    |> ignore

let configureLogging (builder: ILoggingBuilder) =
    builder.AddConsole()
    |> ignore

[<EntryPoint>]
let main args =
    let contentRoot = Directory.GetCurrentDirectory()
    Host.CreateDefaultBuilder(args)
        .ConfigureWebHostDefaults(fun webHostBuilder ->
        webHostBuilder.UseContentRoot(contentRoot)
                      .Configure(Action<IApplicationBuilder> configureApp).ConfigureServices(configureServices)
                      .ConfigureLogging(configureLogging)
        |> ignore).Build().Run()
    0
