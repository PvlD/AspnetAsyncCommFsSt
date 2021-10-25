module RequestSvc


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

   Slug:string
   Timeout:int
   ProductData: string

   Error: string option
}

type Message =

    | SetSlug of string
    | SetTimeout of int 
    | SendRequest
    | RecvRequest of HildenCo.Core.Product
    | Error of string
    | Exception of Exception
    | ClearError
    | Log of string 




let getError m = m.Error
let getErrorStr m = m.Error |> function
                                    | Some  s ->    s
                                    |_-> ""

let getProductData m = m.ProductData
let getSlug m = m.Slug

let getSlugIsValid m = m.Slug.Trim().Length <>0

let init () =
    { Slug ="" ; Timeout=1;ProductData =""; Error = None }, Cmd.none


let update (remote:IProductsSetviceApi) msg model =
    match msg with

    | ClearError ->
        { model with Error = None }, Cmd.none
    | Error msg ->
        //Browser.Dom.console.log ("Error  " + msg)
        { model with Error = Some msg ; ProductData= "" }, Cmd.none
    | Exception x ->
        //Browser.Dom.console.log ("Exception " + x.Message)
        { model with Error = Some x.Message ; ProductData= ""  }, Cmd.none

    | SetSlug  s ->
        //Browser.Dom.console.log ("SetSlug s:" + s )
        { model with Slug = s ; ProductData =""}, Cmd.none

    | SetTimeout  v ->
        //Browser.Dom.console.log ( sprintf "SetTimeout %d" v    )
        { model with Timeout = v }, Cmd.none

    | SendRequest ->
        let cmd = Cmd.OfAsync.either remote.getBySlug (model.Slug,model.Timeout) RecvRequest Exception
        { model with ProductData = "Waiting for data"; }, Cmd.batch[ cmd ; Cmd.ofMsg(Log(" log after SendRequest "))  ]

    | RecvRequest r ->
        
        { model with ProductData =    (Fable.Core.JS.JSON.stringify (r)); }, Cmd.none

    | Log s ->
            Browser.Dom.console.log ( sprintf "Log  %s" s    )
            model , Cmd.none

          
let appStyle = [  
    rule " .width100" [
        Css.width (percent 100) // Streatch list and text box to fit column, looks nicer right aligned
    ]
    rule ".field-label" [
        Css.flexGrow 2// Allow more space for field label
    ]
    rule "label.label" [
        Css.textAlignLeft // To match 7GUI spec
    ]   

    rule ".width60px" [
        Css.width (px 60) 
    ]
    rule ".height100" [
        Css.height (percent 100) 
    ]

    rule ".minHeightColumn" [
        Css.minHeight (px 260) 
        ]

          
]

let errorIsSet (error: string option) = error.IsSome
let productDataIsSet (data:string ) = data.Length <> 0 


module EventHelpers =
    open Browser.Types

    let inputElement (target:EventTarget) = target |> asElement<HTMLInputElement>

    let validity (e : Event) =
        inputElement(e.target).validity

let create() =

    let productsSetviceApi =
       Remoting.createApi ()
       |> Remoting.withRouteBuilder Shared.Route.builder
       |> Remoting.buildProxy<IProductsSetviceApi>


    let model, dispatch = () |> Store.makeElmish init ( update productsSetviceApi) ignore

    let isError : IObservable<bool> = model |> Store.map (getError >> errorIsSet )
    let isSubmitDisabled  : IObservable<bool> = model |> Store.map ( not << getSlugIsValid  )
    
    let timeoutsList: {| timeout:int; description:string |} list  =  List.ofSeq (seq {
                                                                                    for i in 1..5 do
                                                                                        yield {|timeout =i;description = i.ToString() + "s"|}
                                                                                    }
                                                                                )
    let selectedTimeout : IStore<{| timeout:int; description:string |} list  > = Store.make( [ (timeoutsList |> List.head) ])

    bulma.section [
                                disposeOnUnmount [ model]
                                
                                bulma.section [

                                              Bind.el(isError ,fun iserr ->
                                                            if iserr then
                                                                bulma.notification[
                                                                  class' "is-danger"  
                                                                  Html.button[
                                                                      class' "delete"
                                                                      onClick  (fun _ -> dispatch ClearError) [PreventDefault]
                                                                      ]


                                                                  Bind.el  ((model |> Store.map getErrorStr), Html.text)
   
                                                                ]

                                                            else
                                                                Html.div "" 

                                              )
                                              
                                              
                                              bulma.columns[  
                                                    
                                                    bulma.column[
                                                        class' "minHeightColumn"
                                                        bulma.box [    
                                                            bulma.title.h4Is4 [
                                                                                class' "title"
                                                                                Html.text "Request:"
                                                                               ]
                                                            bulma.field.div [
                                                                bulma.fieldLabel [bulma.label "Product Slug:"]
                                                                bulma.fieldBody [
                                                                    bulma.control.div [
                                                                        class' "width100"
                                                                        bulma.input.text [
                                                                        let isInvalid = Store.make false
                                                                        disposeOnUnmount [ isInvalid ]
                                                                        Bind.toggleClass(isInvalid, "is-danger")        
                                                                        on "input" (fun e -> EventHelpers.validity(e).valid |> not |> Store.set isInvalid) []    
                                                                        Attr.placeholder "Product Slug"
                                                                        Bind.attr ("value", model .> getSlug , SetSlug >> dispatch)
                                                                        Attr.required true
                                                                        //Attr.pattern "^\S+$"
                                                                        
                                                                        ]
                                                                    ]
                                                                ]
                                                            ]
                                                            bulma.field.div [
                                                             
                                                             bulma.fieldLabel [bulma.label "Fake Timeout:"]
                                                             bulma.fieldBody [

                                                                              
                                                                               bulma.select  ([
                                                                                               
                                                                                               Attr.size 1
                                                                                               Bind.selected (selectedTimeout ,List.exactlyOne >>  (fun td -> td.timeout) >>   SetTimeout >> dispatch)
                                                                                           ]  @ (timeoutsList   |> List.map (fun n ->
                                                                                                                                   Html.option [
                                                                                                                                           Attr.value n
                                                                                                                                           n.description |> Html.text
                                                                                                                                   ]
                                                                                                                            )
                                                                                                 )
                                                                                           )

                                                                            ]
                                                                            
                                                            ]
                                                            bulma.control.div [
                                                                bulma.button.button [
                                                                    color.isSuccess
                                                                    //Bind.attr ("disabled",isSubmitDisabled)
                                                                    Bind.el  ((isSubmitDisabled |> Store.map id ), Attr.disabled)
                                                                    Html.text "Submit"
                                                                    onClick (fun _ -> dispatch SendRequest) [PreventDefault]
                                                                ]
                                                            ]


                                                        ]


                                                    ]
                                                    

                                                    bulma.column[
                                                        class' "minHeightColumn"
                                                        bulma.box [
                                                            class' "height100" 
                                                            
                                                            bulma.title.h4Is4 [
                                                                Html.text "Response:"
                                                            ]  

                                                            
                                                            Html.div[
                                                                Bind.el  ((model |> Store.map getProductData), Html.text)
                                                                ]
                                                            
                                                        ]
                                                    ]

                                              ]                    

                                ]                      
    ] |> withStyle appStyle
                



   
    

                        
       
