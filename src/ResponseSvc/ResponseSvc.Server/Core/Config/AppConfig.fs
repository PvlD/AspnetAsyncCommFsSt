namespace RequestSvc.Core.Config
open AAC.Core.Config

type AppConfig () = 
            member val MassTransit : MassTransitConfig  = Unchecked.defaultof<MassTransitConfig  > with get, set
        

