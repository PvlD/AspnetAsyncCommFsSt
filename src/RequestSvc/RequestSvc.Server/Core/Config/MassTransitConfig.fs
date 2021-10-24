namespace RequestSvc.Core.Config

type MassTransitConfig () = 
            member val  Host="" with get, set
            member val  Queue="" with get, set
            
        
module MassTransitConfig  =
    let cfgSection = "MassTransit"