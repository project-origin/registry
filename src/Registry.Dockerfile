ARG PROJECT=ProjectOrigin.Registry

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0.401 AS build
ARG PROJECT

WORKDIR /src

COPY ./Protos ./Protos
COPY ./${PROJECT} ./${PROJECT}

RUN dotnet restore ${PROJECT}
RUN dotnet build ${PROJECT} -c Release --no-restore -p:CustomAssemblyName=App
RUN dotnet publish ${PROJECT} -c Release --no-build -p:CustomAssemblyName=App -o /app/publish

# ------- production image -------
FROM mcr.microsoft.com/dotnet/aspnet:8.0.8-jammy-chiseled-extra AS production

WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 5000

ENTRYPOINT ["dotnet", "App.dll"]
