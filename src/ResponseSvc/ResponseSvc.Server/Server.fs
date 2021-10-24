module Server

open System
open Fable.Remoting.Server
open Fable.Remoting.Giraffe
open Saturn

open HildenCo.Core
open Shared
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection

open MassTransit
open MassTransit.RabbitMqTransport
open ResponseSvc.Core.Consumers
open GreenPipes
open Microsoft.AspNetCore.Http
open ResponseSvc.Core.Services



let productsSetviceApi (ctx: HttpContext)  : IProductsSetviceApi =

    { 
      allProducts = fun () -> async {
      
                       let svc = ctx.RequestServices.GetService<ICatalogSvc>()
                       return svc.GetAllProducts()
      
                  }
    }

let webApp =
    Remoting.createApi ()
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.fromContext  productsSetviceApi 
    |> Remoting.buildHttpHandler


let configCatalogSvc (s:IServiceCollection)=
                    s.AddTransient<ICatalogSvc, CatalogSvc>()

let configMassTransit (s:IServiceCollection)=
        let cfg = s.BuildServiceProvider() .GetService<IConfiguration>()
        let massTransitConfig = cfg.GetSection(RequestSvc.Core.Config.MassTransitConfig.cfgSection).Get< RequestSvc.Core.Config.MassTransitConfig>()

        s.AddMassTransit( fun  x ->
            
                 x.AddConsumer<ProductInfoRequestConsumer>() |> ignore

                 x.AddBus(fun context ->
                                    Bus.Factory.CreateUsingRabbitMq(fun c ->
                                            c.Host(massTransitConfig.Host)
                                            c.ReceiveEndpoint ( massTransitConfig.Queue,
                                                               fun  (e:IRabbitMqReceiveEndpointConfigurator) ->
                                                                                             e.PrefetchCount <- 16              
                                                                                             e.UseMessageRetry(fun r -> r.Interval(2, 3000) |> ignore  )
                                                                                             e.ConfigureConsumer<ProductInfoRequestConsumer>(context)
                                                                                             
                                                                                        
                                                               )
                                    )  
                            )
                                

                
            ) |> ignore


        s.AddMassTransitHostedService() |> ignore


        s


let app =
    application {
        url "http://0.0.0.0:8085"
        use_router webApp
        memory_cache
        use_static "public"
        use_gzip
        service_config configCatalogSvc 
        service_config configMassTransit 


    }

run app
