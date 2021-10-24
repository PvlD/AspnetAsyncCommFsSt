namespace Shared

open System

module Route =
    let builder typeName methodName =
        sprintf "/api/%s/%s" typeName methodName

type IProductsSetviceApi =
    {
       getBySlug : string * int -> Async<HildenCo.Core.Product>
     }
