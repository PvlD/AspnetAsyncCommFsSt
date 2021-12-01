module Server

open System
open Fable.Remoting.Server
open Fable.Remoting.Giraffe
open Saturn


open Shared
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection

open MassTransit
open MassTransit.RabbitMqTransport
open GreenPipes
open Microsoft.AspNetCore.Http
open ResponseSvc.Core.Services.ProductsSetvice
open HildenCo.Core.Contracts

open AAC.Core.Config
open MassTransit.Azure.ServiceBus.Core




let webApp =
    Remoting.createApi ()
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.fromContext  productsSetviceApi 
    |> Remoting.buildHttpHandler



let configMassTransit (s:IServiceCollection)=
        let cfg = s.BuildServiceProvider() .GetService<IConfiguration>()
        let massTransitConfig = cfg.GetSection(MassTransitConfig.cfgSection).Get< MassTransitConfig>()

        s.AddMassTransit( fun  x ->
                    
                        x.AddBus( fun context ->


                            match SelectedBus.fromString  massTransitConfig.SelectedBus with
                                |SelectedBus.Rabbitmq ->
                                                    Bus.Factory.CreateUsingRabbitMq( fun c ->
                                                    c.Host(massTransitConfig.Rabbitmq.Host);
                                                    c.ConfigureEndpoints(context);
                                                    )

                                |SelectedBus.AzureServiceBus ->
                                                    Bus.Factory.CreateUsingAzureServiceBus( fun c ->
                                                    c.Host(massTransitConfig.AzureServiceBus.ConnectionStrings);
                                                    c.ConfigureEndpoints(context);
                                                    )

                                        
                                |_-> failwith $"unknowm SelectedBus:{massTransitConfig.SelectedBus}  in config"



                            
                        )

                        x.AddRequestClient<ProductInfoRequest>();
                        
                    ) |> ignore

        s.AddMassTransitHostedService() |> ignore

        s


let app =
    application {
        //url "http://0.0.0.0:8086"
        use_router webApp
        memory_cache
        use_static "public"
        use_gzip
        service_config configMassTransit 


    }

run app
