# AspnetAsyncCommFsSt

converted https://github.com/hd9/aspnet-async-communication to F# : Sutil , Saturn



from slodudion folder:

dotnet tool restore

dotnet paket install

dotnet run 

wait when completed.

then open in browser:

http://localhost:8080/#home

http://localhost:8081/#home



![](/images/img1.png)


![](/images/img2.png)

![](/images/img3.png)


## Deployment to Azure 

from slodudion folder

for RabbitMQ : dotnet run -- Azure 

for Azure Service Bus : dotnet  run  -t Azure -e bus=azureservicebus


![](/images/img4.png)

![](/images/AzBus.png)


wait when completed.

change "hosts" file.

responseSvc.com

requestSvc.com


![](/images/img5.png)

![](/images/img6.png)

![](/images/img7.png)





