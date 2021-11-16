module AzureT 
open Farmer
open Farmer.Builders
open Farmer.Arm

    
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



        let net = vnet {
            name "aacfsst-vnet"
            build_address_spaces [
                addressSpace {
                    space "10.28.0.0/16"
                    subnets [
                        subnetSpec {
                                          name "gw"
                                          size 24
                                      }
                        subnetSpec {
                            name "responseSvc"
                            size 24
                            add_delegations [
                                SubnetDelegationService.ContainerGroups
                            ]
                        }
                        subnetSpec {
                            name "requestSvc"
                            size 24
                            add_delegations [
                                SubnetDelegationService.ContainerGroups
                            ]
                        }
                        subnetSpec {
                            name "rabbit"
                            size 24
                            add_delegations [
                                SubnetDelegationService.ContainerGroups
                            ]
                        }

                    ]
                }
            ]
        }
        let msi = createUserAssignedIdentity "aacfsst-msi"

        let backendPoolNameReponseSvc = ResourceName "responseSvc-pool"
        let backendPoolNameRequestSvc = ResourceName "requestSvc-pool"
        let backendPoolNameRabbit = ResourceName "rabbit-pool"

        let myAppGateway =
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

            let backendPoolRabbit =
                appGatewayBackendAddressPool {
                    name backendPoolNameRabbit.Value
                    add_backend_addresses [
                        backend_ip_address "10.28.3.4"
            
                    ]
                }



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
                add_backend_address_pools [ backendPoolRequestSvc; backendPoolReponseSvc; backendPoolRabbit  ]
                add_backend_http_settings_collection [ backendSettings ]
                add_request_routing_rules [ routingRule_responseSvc; routingRule_requestSvc ]
                add_probes [ healthProbe ]
                depends_on net
           }


        let getARM (  imageRegistryServer, imageRegistryUsername , imageRegistryPasswordKey ) =

            let   registry_credentials = [{ ImageRegistryCredential.Server= imageRegistryServer + ".azurecr.io"; Username=imageRegistryUsername ; Password= SecureParameter imageRegistryPasswordKey }]
            let   rabbit_env_var = ("MassTransit__Host","rabbitmq://10.28.3.4")
            arm {
                location Cfg.location
                add_resources [
            
                    msi
                    net
                    myAppGateway
                    networkProfile {
                        name "responseSvc-netprofile"
                        vnet net.Name.Value
                        subnet net.Subnets.[1].Name.Value
                    }
                    networkProfile {
                        name "requestSvc-netprofile"
                        vnet net.Name.Value
                        subnet net.Subnets.[2].Name.Value
                    }
                    networkProfile {
                        name "rabbit-netprofile"
                        vnet net.Name.Value
                        subnet net.Subnets.[3].Name.Value
                    }

                    containerGroup {
                        add_registry_credentials registry_credentials
                        name "aci-responseSvc"
                        add_instances [
                            containerInstance {
                                name "responseSvc"
                                image  $"{imageRegistryServer}.azurecr.io/responsesvc:latest"
                                add_internal_ports [ 80us ]
                                env_vars [rabbit_env_var]
                            }
                        ]
                        network_profile "responseSvc-netprofile"
                    }
                    containerGroup {
                        add_registry_credentials registry_credentials
                        name "aci-requestSvc"
                        add_instances [
                            containerInstance {
                                name "requestSvc"
                                image $"{imageRegistryServer}.azurecr.io/requestsvc:latest"
                                add_internal_ports [ 80us ]
                                env_vars [rabbit_env_var]
                            }
                        ]
                        network_profile "requestSvc-netprofile"
                    }
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
            } 
                //|> Writer.quickWrite "applicationGateway"



let doIt( azureResourceGroup)=


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



        let deploymentGw = ApplicationGateway.getARM(registry_name , registry_username_val ,registry_password_key  )

        deploymentGw
        |> Deploy.execute azureResourceGroup [(registry_password_key ,registry_password_val)]
        |> printfn "%A"

        //deploymentGw |> Writer.quickWrite "output"