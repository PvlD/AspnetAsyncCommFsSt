namespace AAC.Core.Config



type SelectedBus =
    |Unknown
    |Rabbitmq
    |AzureServiceBus
    with 
    static member fromString (s:string) = 
                    
                    let  (|Unknown|Rabbitmq|AzureServiceBus |) (v : string) =
                                           match v.Trim().ToLower() with 
                                           |"rabbitmq" -> Rabbitmq
                                           |"azureservicebus" -> AzureServiceBus 
                                           |_-> Unknown

                    match s with  
                    |Unknown -> SelectedBus.Unknown
                    |Rabbitmq -> SelectedBus.Rabbitmq
                    |AzureServiceBus  -> SelectedBus.AzureServiceBus

    static member toString (v:SelectedBus) = 
                    v.ToString()




type RabbitmqConfig() =
        member val  Host="" with get, set
        



type AzureServiceBusConfig() =
        member val  ConnectionStrings="" with get, set
        




type MassTransitConfig () =

            let mutable _rabbitmq  =  new RabbitmqConfig ()
            let mutable _azureServiceBus  =  new AzureServiceBusConfig ()


            member val  Queue="" with get, set


            member _.Rabbitmq  with get  ()  = _rabbitmq
                               and  set v = _rabbitmq  <- v
                                     
            member _.AzureServiceBus  with get  ()  = _azureServiceBus
                                      and  set v = _azureServiceBus  <- v

            
            member val SelectedBus = ""  with get, set
        
module MassTransitConfig  =
    let cfgSection = "MassTransit"




