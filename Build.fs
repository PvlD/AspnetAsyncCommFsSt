open Fake.Core
open Fake.IO
open Farmer
open Farmer.Builders

open Helpers

initializeContext([])



Target.create "Bundle" (fun _ ->
    [
        "ResponseSvc", dotnet $" run -- Bundle " Cfg.responseSvcPath
        "RequestSvc", dotnet $" run -- Bundle " Cfg.requestSvcPath
         ]
    |> runParallel
)

Target.create "Azure" (fun _ ->
    AzureT.doIt(Cfg.azureResourceGroup)
    )    


Target.create "Run" (fun _ ->
    [
        "ResponseSvc", dotnet $" run -- Run " Cfg.responseSvcPath
        "RequestSvc", dotnet $" run -- Run " Cfg.requestSvcPath
         ]
    |> runParallel
)


open Fake.Core.TargetOperators

let dependencies = [
    
    "Bundle"
        ==> "Azure"

    "Run"
]

[<EntryPoint>]
let main args = runOrDefault args