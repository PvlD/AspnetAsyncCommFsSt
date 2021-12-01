module Cfg
open Fake.IO
open Farmer

let azureResourceGroup = "AACFsStRG" 
let registry_name = "aacfsstreg" // change 
let location = Location.WestUS2
let azureServiceBus_name ="AspnetAsyncCommFsStBus" // change 


let responseSvcPath = Path.getFullName "src/ResponseSvc"
let requestSvcPath = Path.getFullName "src/RequestSvc"


