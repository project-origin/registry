ARG PROJECT=ProjectOrigin.Registry

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:9.0.200 AS build
ARG PROJECT

WORKDIR /builddir

COPY ./Directory.Build.props ./Directory.Build.props
COPY ./protos ./protos
COPY ./src ./src

RUN dotnet restore ./src/${PROJECT}
RUN dotnet build ./src/${PROJECT} -c Release --no-restore -p:CustomAssemblyName=Registry
RUN dotnet publish ./src/${PROJECT} -c Release --no-build -p:CustomAssemblyName=Registry -o /app/publish

# ------- production image -------
FROM mcr.microsoft.com/dotnet/aspnet:9.0-noble AS production

WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 5000
EXPOSE 5001

ENV ReturnComittedForFinalized=true

ENTRYPOINT ["dotnet", "Registry.dll"]
