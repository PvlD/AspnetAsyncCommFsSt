module Cfg
open Fake.IO
open Farmer

let azureResourceGroup = "AACFsStRG" 
let registry_name = "aacfsstreg" 
let location = Location.CentralUS


let responseSvcPath = Path.getFullName "src/ResponseSvc"
let requestSvcPath = Path.getFullName "src/RequestSvc"


