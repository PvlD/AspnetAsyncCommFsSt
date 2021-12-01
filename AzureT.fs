module AzureT
open Fake.Core

open Farmer
open Farmer.Builders
open Farmer.Arm


type SelectedBus =
    |Rabbitmq
    |AzureServiceBus
    with
    

    static member fromString (s:string) = 
                    
                    let  (|Rabbitmq|AzureServiceBus |) (v : string) =
                                           match v.Trim().ToLower() with 
                                           |"rabbitmq" -> Rabbitmq
                                           |"azureservicebus" -> AzureServiceBus 
                                           |_-> failwith $"Unknown Bus type {v}"

                    match s with  
                    |Rabbitmq -> SelectedBus.Rabbitmq
                    |AzureServiceBus  -> SelectedBus.AzureServiceBus

    static member toString (v:SelectedBus) = 
                    v.ToString()

    static  member   envVarName = "bus"
    
    static member selectedBus() =
                        Environment.environVarOrNone SelectedBus.envVarName
                        |> function
                          | Some s ->  SelectedBus.fromString s
                          |       _->  let r = SelectedBus.Rabbitmq
                                       Trace.logToConsole ($"Bus not set {r} is assumed" ,  Trace.EventLogEntryType.Warning)
                                       r


    
module ContainerRegistry =

        let getARM ( registry_name , location_) =
        
            
            let myRegistry = containerRegistry {
                name registry_name
                sku ContainerRegistry.Basic
                enable_admin_user
            }
        
        
            arm {
                location location_
                add_resources [
                myRegistry            
                ]
                output "registry_username" myRegistry.Username
                output "registry_password" myRegistry.Password
                output "registry_login_server" myRegistry.LoginServer
            } 
        
module ApplicationGateway =
        open Farmer.Network
        open Farmer.ApplicationGateway



        let net (selctedBus:SelectedBus) = vnet {

            name "aacfsst-vnet"
            build_address_spaces [
                addressSpace {
                    space "10.28.0.0/16"
                    subnets [
                        
                            yield subnetSpec {
                                              name "gw"
                                              size 24
                                          }
                            yield subnetSpec {
                                name "responseSvc"
                                size 24
                                add_delegations [
                                    SubnetDelegationService.ContainerGroups
                                ]
                                }
                            
                            yield subnetSpec {
                                name "requestSvc"
                                size 24
                                add_delegations [
                                    SubnetDelegationService.ContainerGroups
                                ]
                            }
                            yield!   match selctedBus with 
                                                    | SelectedBus.Rabbitmq ->
                                                                        [subnetSpec {
                                                                            name "rabbit"
                                                                            size 24
                                                                            add_delegations [
                                                                                SubnetDelegationService.ContainerGroups
                                                                            ]
                                                                        }
                                                                        ]
                                                    |_-> []

                    ]
                }
            ]
        }
        let msi = createUserAssignedIdentity "aacfsst-msi"

        let backendPoolNameReponseSvc = ResourceName "responseSvc-pool"
        let backendPoolNameRequestSvc = ResourceName "requestSvc-pool"
        let backendPoolNameRabbit = ResourceName "rabbit-pool"

        let myAppGateway (selctedBus:SelectedBus , net:VirtualNetworkConfig) =
            let gwIp =
                gatewayIp {
                    name "aacfsst-gw-ip"
                    link_to_subnet net.Name net.Subnets.[0].Name
                }
            let frontendIp =
                frontendIp {
                    name "aacfsst-gw-fe-ip"
                    public_ip "aacfsst-gw-pip"
                }
            let frontendPort =
                frontendPort {
                    name "port-80"
                    port 80
                }
            let listener_responseSvc =
                httpListener {
                    name "http-listener-responseSvc"
                    frontend_ip frontendIp
                    frontend_port frontendPort
                    backend_pool backendPoolNameReponseSvc.Value
            
                }

            let listener_responseSvc  = { listener_responseSvc with    HostNames = ["responseSvc.com"] ; RequireServerNameIndication= false }

            let listener_requestSvc =
                httpListener {
                    name "http-listener-requestSvc"
                    frontend_ip frontendIp
                    frontend_port frontendPort
                    backend_pool backendPoolNameRequestSvc.Value
                }
            let listener_requestSvc  = { listener_requestSvc with    HostNames = ["requestSvc.com"] ; RequireServerNameIndication= false }

            let backendPoolReponseSvc =
                appGatewayBackendAddressPool {
                    name backendPoolNameReponseSvc.Value
                    add_backend_addresses [
                        backend_ip_address "10.28.1.4"
                
                    ]
                }
            let backendPoolRequestSvc =
                appGatewayBackendAddressPool {
                    name backendPoolNameRequestSvc.Value
                    add_backend_addresses [
                        backend_ip_address "10.28.2.4"
            
                    ]
                }


            let   backendPools  =[
                                  yield backendPoolReponseSvc 

                                  yield backendPoolRequestSvc

                                  yield!    selctedBus |> function
                                                                |SelectedBus.Rabbitmq ->
                                                                                        [
                                                                                        appGatewayBackendAddressPool {
                                                                                            name backendPoolNameRabbit.Value
                                                                                            add_backend_addresses [
                                                                                                backend_ip_address "10.28.3.4"
            
                                                                                            ]
                                                                                        }

                                                                                        ]
                                                                |_-> []

                                 ]


            let healthProbe =
                appGatewayProbe {
                    name "tst-probe"
                    host "localhost"
                    path "/"
                    port 80
                    protocol Protocol.Http
                }
            let backendSettings =
                backendHttpSettings {
                    name "bp-default-web-80-web-80"
                    port 80
                    probe healthProbe
                    protocol Protocol.Http
                    request_timeout 10<Seconds>
                }
            let routingRule_responseSvc =
                basicRequestRoutingRule {
                    name "web-front-to-responseSvc"
                    http_listener listener_responseSvc
                    backend_address_pool backendPoolReponseSvc
                    backend_http_settings backendSettings
                }

            let routingRule_requestSvc =
                basicRequestRoutingRule {
                    name "web-front-to-requestSvc"
                    http_listener listener_requestSvc
                    backend_address_pool backendPoolRequestSvc
                    backend_http_settings backendSettings
                }


    
            appGateway {
                name "tst-gw"
                sku_capacity 2
                add_identity msi
                add_ip_configs [ gwIp ]
                add_frontends [ frontendIp ]
                add_frontend_ports [ frontendPort ]
                add_http_listeners [ listener_requestSvc; listener_responseSvc ]
                add_backend_address_pools backendPools  
                add_backend_http_settings_collection [ backendSettings ]
                add_request_routing_rules [ routingRule_responseSvc; routingRule_requestSvc ]
                add_probes [ healthProbe ]
                depends_on net
           }

        

        let getARM ( selctedBus, imageRegistryServer, imageRegistryUsername , imageRegistryPasswordKey , serviceBus_connectionString_val : string option ) =


            let   registry_credentials = [{ ImageRegistryCredential.Server= imageRegistryServer + ".azurecr.io"; Username=imageRegistryUsername ; Password= SecureParameter imageRegistryPasswordKey }]

            let   bus_env_var =  match selctedBus with
                                    |SelectedBus.Rabbitmq -> [("MassTransit__SelectedBus",SelectedBus.Rabbitmq.ToString());("MassTransit__Rabbitmq__Host","rabbitmq://10.28.3.4")]
                                    |SelectedBus.AzureServiceBus ->  [("MassTransit__SelectedBus",SelectedBus.AzureServiceBus.ToString());("MassTransit__AzureServiceBus__ConnectionStrings", Option.get serviceBus_connectionString_val)]

            

            let net = net(selctedBus)
            let myAppGateway = myAppGateway(selctedBus,net)
            let networkProfiles = [
                                   yield networkProfile {
                                       name "responseSvc-netprofile"
                                       vnet net.Name.Value
                                       subnet net.Subnets.[1].Name.Value
                                   } 
                                   yield networkProfile {
                                       name "requestSvc-netprofile"
                                       vnet net.Name.Value
                                       subnet net.Subnets.[2].Name.Value
                                   }
                                   yield!  selctedBus |> function
                                                        |SelectedBus.Rabbitmq -> [networkProfile {
                                                                   name "rabbit-netprofile"
                                                                   vnet net.Name.Value
                                                                   subnet net.Subnets.[3].Name.Value
                                                               }
                                                               ]
                                                        |_->[]

                                  ] |> Seq.ofList |> Seq.cast<IBuilder> 
            let containerGroups = [

                                   yield  containerGroup {
                                       add_registry_credentials registry_credentials
                                       
                                       name "aci-requestSvc"
                                       add_instances [
                                           containerInstance {
                                               name "requestSvc"
                                               image $"{imageRegistryServer}.azurecr.io/requestsvc:latest"
                                               add_internal_ports [ 80us ]
                                               env_vars bus_env_var
                                           }
                                       ]
                                       network_profile "requestSvc-netprofile"
                                   }
 

                                   yield containerGroup {
                                       add_registry_credentials registry_credentials
                                       
                                       name "aci-responseSvc"
                                       add_instances [
                                           containerInstance {
                                               name "responseSvc"
                                               image  $"{imageRegistryServer}.azurecr.io/responsesvc:latest"
                                               add_internal_ports [ 80us ]
                                               env_vars bus_env_var
                                           }
                                       ]
                                       network_profile "responseSvc-netprofile"
                                   }
                                   yield!  selctedBus |> function
                                                           |SelectedBus.Rabbitmq -> ([
                                                                                           containerGroup {
                                                                                           
                                                                                           name "aci-rabbit"
                                                                                           add_instances [
                                                                                               containerInstance {
                                                                                                   name "rabbit"
                                                                                                   image $"docker.io/rabbitmq:3"
                                                                                                   add_internal_ports [ 5672us ]
                                                                                               }
                                                                                           ]
                                                                                           network_profile "rabbit-netprofile"
                                                                                       }

                                                                  ] 
                                                                  )
                                                           |_->[]

                                  ] |> Seq.ofList |> Seq.cast<IBuilder> 
                                
            arm {
                location Cfg.location
                add_resources [
                    yield!   ([
                               msi 
                               net
                               myAppGateway
                              ] : IBuilder list
                              )
                    yield! networkProfiles 
                    yield! containerGroups

                ]
            } 
                //|> Writer.quickWrite "applicationGateway"



let doIt( azureResourceGroup)=

        let selctedBus = SelectedBus.selectedBus()


        let registry_name = Cfg.registry_name
        let location = Cfg.location

        let deploymentContainerRegistry = ContainerRegistry.getARM(registry_name,location)

        let deploymentContainerRegistryOut =
                    deploymentContainerRegistry
                    |> Deploy.execute azureResourceGroup []

        let registry_password_key =  "registry_password"
        let registry_password_val = deploymentContainerRegistryOut.[registry_password_key]

        let registry_username_key =  "registry_username"
        let registry_username_val = deploymentContainerRegistryOut.[registry_username_key]

        let registry_login_server_key =  "registry_login_server"
        let registry_login_server_val = deploymentContainerRegistryOut.[registry_login_server_key]

        printfn "registry_password:%s   registry_username:%s  :%s"  registry_password_val registry_username_val registry_login_server_val


        AzureACI.loginRegistry registry_login_server_val 
        AzureACI.imageBuildDeployByDocker registry_login_server_val "responsesvc" Cfg.responseSvcPath
        AzureACI.imageBuildDeployByDocker registry_login_server_val "requestsvc" Cfg.requestSvcPath


        let azureServiceBus_data =   match selctedBus   with
                                                    | SelectedBus.AzureServiceBus ->
                                                                               let deployment = ABusT.getARM ( location)
                                              
                                                                               let deploymentOut =
                                                                                           deployment
                                                                                           |> Deploy.execute azureResourceGroup []
                                           
                                                                               let serviceBus_connectionString_key =  "serviceBus_connectionString"
                                                                               let serviceBus_connectionString_val = deploymentOut.[serviceBus_connectionString_key]

                                                                               Some (serviceBus_connectionString_key ,serviceBus_connectionString_val)
                                                                               //printfn "serviceBus_connectionString:%s   "  serviceBus_connectionString_val
                                
                                                    |_-> None

        

        let deploymentGw = ApplicationGateway.getARM(selctedBus, registry_name , registry_username_val ,registry_password_key,(azureServiceBus_data |> function
                                                                                                                                                            | Some (_,v) -> Some(v)
                                                                                                                                                            |_-> None
                                                                                                                               )
        )       

        deploymentGw
        |> Deploy.execute azureResourceGroup [
                                                yield (registry_password_key ,registry_password_val)

                                             ]
        |> printfn "%A"

        //deploymentGw |> Writer.quickWrite "output"