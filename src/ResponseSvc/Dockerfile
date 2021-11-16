#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
WORKDIR /app
EXPOSE 80


FROM base AS final
WORKDIR /app
ADD ./deploy .
ENTRYPOINT ["dotnet", "ResponseSvc.Server.dll"]   