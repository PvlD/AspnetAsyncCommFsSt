open Fake.Core
open Fake.IO
open Farmer
open Farmer.Builders
open Fake.Core.TargetOperators

open Helpers





let init (args) =

    initializeContext(args)



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


    

    let dependencies = [
    
        "Bundle"
            ==> "Azure"

        "Run"
    ]
    ()

[<EntryPoint>]
let main args =

    init((args |> List.ofArray))
    try
           Target.runOrDefault "Run"
           0
    with e ->
           printfn "%A" e
           1
