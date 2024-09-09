ARG PROJECT=ProjectOrigin.Registry

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0.401 AS build
ARG PROJECT

WORKDIR /builddir

COPY ./Directory.Build.props ./Directory.Build.props
COPY ./protos ./protos
COPY ./src ./src

RUN dotnet restore ./src/${PROJECT}
RUN dotnet build ./src/${PROJECT} -c Release --no-restore -p:CustomAssemblyName=App
RUN dotnet publish ./src/${PROJECT} -c Release --no-build -p:CustomAssemblyName=App -o /app/publish

# ------- production image -------
FROM mcr.microsoft.com/dotnet/aspnet:8.0.4-jammy-chiseled-extra AS production

WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 5000
EXPOSE 5001

ENTRYPOINT ["dotnet", "App.dll"]