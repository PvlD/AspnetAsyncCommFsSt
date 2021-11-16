module AzureACI
open Fake.Core
open Fake.IO


let private  docker  = (Helpers.createProcess "Docker.exe" )

let loginRegistry registry_login_server  =
                printfn "start login   registry_login_server:%s "  registry_login_server  
                let r = Farmer.Deploy.Az.az $"acr login --name  {registry_login_server}"
                match r  with 
                                   | Ok  s ->     printfn "login  success :%s " s  
                                                  ()
                                   | Error s   -> failwith s



let imageBuildDeployByDocker  registry_login_server image_name path =

       printfn "start build image  registry_login_server:%s image_name:%s path:%s"  registry_login_server  image_name path

       Helpers.run  docker   $"build -t \"{image_name}\" . " path

       Helpers.run  docker   $"tag  \"{image_name}\"  {registry_login_server}/{image_name}  " path

       Helpers.run  docker   $"push  {registry_login_server}/{image_name}  " path


       


