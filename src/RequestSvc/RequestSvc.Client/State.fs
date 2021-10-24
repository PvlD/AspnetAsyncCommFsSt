module State

open Types
open Sutil
open System

open Shared 
open Browser.Dom

type Message =
    | SetPage of Page
    | SetException of Exception

let init () : Model * Cmd<Message> = { Page = Home; Error = None ;  }, Cmd.none

let update  (msg : Message) (model : Model) : Model * Cmd<Message> =
    //Browser.Dom.console.log($"{msg}")
    match msg with

    |SetException x ->
        { model with Error = Some  x.Message }, Cmd.none

    //|SetMessage m ->
    //    { model with Message = m }, Cmd.none

    |SetPage p ->
        window.location.href <- "#" + (string p).ToLower()
        { model with Page = p; Error = None }, Cmd.none

