module  ResponseSvc.Core.Services.ProductsSetvice

open System
open MassTransit

open HildenCo.Core.Contracts
open Microsoft.Extensions.DependencyInjection
open Microsoft.AspNetCore.Http
open Shared


let productsSetviceApi (ctx: HttpContext)  : IProductsSetviceApi =

                                                                { 
                                                                    getBySlug =   fun ( slug:string  , timeout:int) -> async {
                                                                 
                                                                                 let client = ctx.RequestServices.GetService<IRequestClient<ProductInfoRequest>>();
                                                                 
                                                                                 use   request = client.Create(new ProductInfoRequest(Slug = slug, Delay = timeout) )
                                                                                 let! response = request.GetResponse<ProductInfoResponse>() |> Async.AwaitTask
                                                                                 return response.Message.Product;
                                                                                 
                                                                                 }
                                                                }


