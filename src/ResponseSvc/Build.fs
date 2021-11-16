open Fake.Core
open Fake.IO
open Farmer
open Farmer.Builders

open Helpers

initializeContext()

let packagePath = Path.getFullName  "../../"
let sharedPath = Path.getFullName "ResponseSvc.Shared"
let serverPath = Path.getFullName "ResponseSvc.Server"
let clientPath = Path.getFullName "ResponseSvc.Client"
let deployPath = Path.getFullName "deploy"
let sharedTestsPath = Path.getFullName "tests/Shared"
let serverTestsPath = Path.getFullName "tests/Server"
let clientTestsPath = Path.getFullName "tests/Client"

Target.create "Clean" (fun _ ->
    Shell.cleanDir deployPath
    run dotnet "fable clean --yes" clientPath // Delete *.fs.js files created by Fable
)

Target.create "InstallClient" (fun _ -> run npm "install" packagePath)

Target.create "Bundle" (fun _ ->
    [ "server", dotnet $"publish -c Release -o \"{deployPath}\"" serverPath
      "client", dotnet "fable -o output -s --run webpack -p" clientPath
      ]
    |> runParallel
)


Target.create "Run" (fun _ ->
    run dotnet "build" sharedPath
    [ 
      "server", dotnet "watch run" serverPath
      "client", dotnet "fable watch -o output -s --run webpack-dev-server" clientPath 
     ]
    |> runParallel
)



Target.create "Format" (fun _ ->
    run dotnet "fantomas . -r" "src"
)

open Fake.Core.TargetOperators

let dependencies = [

    "Clean"
        ==> "InstallClient"
        ==> "Bundle"

    "Clean"
        ==> "InstallClient"
        ==> "Run"


]

[<EntryPoint>]
let main args = runOrDefault args