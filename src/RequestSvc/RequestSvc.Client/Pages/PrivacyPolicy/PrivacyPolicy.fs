module PrivacyPolicy

open Sutil
open Sutil.DOM
open Sutil.Bulma
open Sutil.Attr
open Sutil.Html
open System

let create () =
    bulma.section [
        Html.h2 [ class' "title is-2"; text "Privacy Policy" ]
        el "article" [
            class' "message is-info"
            Html.div [
                class' "message-body"
                text "Use this page to detail your site's privacy policy."
            ]
        ]
    ]