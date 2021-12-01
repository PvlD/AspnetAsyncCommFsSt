module ABusT


open Farmer
open Farmer.Builders
open Farmer.ServiceBus




let getARM( location_)=

    let serviceBus = serviceBus {
        name Cfg.azureServiceBus_name
        sku Standard
        add_queues [
            queue { name "responsesvc" }

        ]
    }
    

    arm {
        location location_
        add_resource serviceBus
        output "serviceBus_connectionString" serviceBus.NamespaceDefaultConnectionString
        //output "defaultSharedAccessPolicyPrimaryKey" myServiceBus.DefaultSharedAccessPolicyPrimaryKey
    }

    