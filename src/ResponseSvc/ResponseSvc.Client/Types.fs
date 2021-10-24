module Types

open System

open HildenCo.Core


let private strCaseEq s1 s2 = String.Equals(s1, s2, StringComparison.CurrentCultureIgnoreCase)

type Page =
    | Home
    | PrivacyPolicy
    with
        static member All = [
            "Home", Home
            "PrivacyPolicy", PrivacyPolicy
            ]
        static member Find (name : string) =
                        Page.All
                        |> List.tryFind (fun (pname,page) -> strCaseEq pname name)
                        |> Option.map snd


                          

type Model =
    {
        Page: Page
        
        Error: string option
        
    }


// Model helpers

let getPage m = m.Page
let getErrorMessage m = m.Error
