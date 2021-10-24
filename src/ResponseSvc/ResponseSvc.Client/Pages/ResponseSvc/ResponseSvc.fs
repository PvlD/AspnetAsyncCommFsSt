module ResponseSvc


open Fable.Remoting.Client

open System

open Sutil
open Sutil.DOM
open Sutil.Attr
open Sutil.Bulma
open type Feliz.length


open Sutil.Styling

open HildenCo.Core
open Shared


type Model = {
   AllProducts: Product list  option 
   Error: string option
}

type Message =
    | GotAllProducts of Product list
    | GetAllProducts 
    | Error of string
    | Exception of Exception
    | ClearError





let getError m = m.Error
let getData m = m.AllProducts


let init () =
    { AllProducts = None ; Error = None }, Cmd.ofMsg  GetAllProducts 


let update (remote:IProductsSetviceApi) msg model =
    match msg with
    | ClearError ->
        { model with Error = None }, Cmd.none
    | Error msg ->
        Browser.Dom.console.log ("Error  " )
        { model with Error = Some msg}, Cmd.none
    | Exception x ->
        Browser.Dom.console.log ("Exception " )
        { model with Error = Some x.Message}, Cmd.none

    | GetAllProducts ->
        Browser.Dom.console.log ("GetAllProducts " )
        let cmd = Cmd.OfAsync.either remote.allProducts () GotAllProducts Exception
        { model with AllProducts = None}, cmd

    | GotAllProducts  allProducts ->
           Browser.Dom.console.log ("GotAllProducts  allProducts:" )
           { model with AllProducts = Some allProducts }, Cmd.none



let appStyle = [
    rule "div.select, select, .width100" [
        Css.width (percent 100) // Streatch list and text box to fit column, looks nicer right aligned
    ]
    rule ".field-label" [
        Css.flexGrow 2// Allow more space for field label
    ]
    rule "label.label" [
        Css.textAlignLeft // To match 7GUI spec
    ]
]

let errorIsSet (error: string option) = error.IsSome
let dataIsSet (data: Product list  option ) = data.IsSome


let create() =

    


    let productsSetviceApi =
       Remoting.createApi ()
       |> Remoting.withRouteBuilder Shared.Route.builder
       |> Remoting.buildProxy<IProductsSetviceApi>


    let model, dispatch = () |> Store.makeElmish init ( update productsSetviceApi) ignore

    let isError : IObservable<bool> = model |> Store.map (getError >> errorIsSet )
    let isData : IObservable<bool> = model |> Store.map (getData >> dataIsSet )

    let isDataOrError = Observable.zip isError isData

    bulma.section [
        disposeOnUnmount [ model]

        Bind.el (isDataOrError, fun (isError,isData)    ->
                match isError,isData with
                | true,_ -> 
                            Bind.el(model,fun m -> Html.text ("Error " + m.Error.Value )                              
                            )    
                                
                | false,true ->
                            bulma.section [
                                                       bulma.title.h3 "Here's our list of products:"
                     
                                                       bulma.table [
                                                           table.isFullwidth
                                                           Html.thead [
                                                                        Html.tr [
                                                                                     Html.th [  Attr.style "min-width:100px" ; Html.text  "#"     ]
                                                                                     Html.th [   Html.text  "Name"     ]
                                                                                     Html.th [   Html.text  "Description"     ]
                                                                                     Html.th [   Html.text  "Price"     ]
                                                                                 ]
                                                                     ]


                                                                 
                                                           Bind.el(model,fun m ->
                                                                                               Html.tbody 
                                                                                                        (m.AllProducts.Value |> List.map (fun row ->
                                                                                                                                                    Html.tr [
                                                                                                                                                      Html.td row.Slug
                                                                                                                                                      Html.td row.Name
                                                                                                                                                      Html.td row.Description
                                                                                                                                                      Html.td (row.Currency + " " +  row.Price.ToString())
                                                                                                                                                    ]
                                                                                                                                                   )
                                                                                                        )
                                                                                   
                                                                        
                                                                                   )   
                                               
                                                        
                                                      ]

                            ]   

                |_,_->  Html.text  "No products"   
                )

    ]
    
                            
    |> withStyle appStyle
