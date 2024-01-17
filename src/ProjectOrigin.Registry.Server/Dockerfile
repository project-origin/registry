FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0.101 AS build

WORKDIR /src

COPY ./ProjectOrigin.Registry.Server ./ProjectOrigin.Registry.Server
COPY ./ProjectOrigin.VerifiableEventStore ./ProjectOrigin.VerifiableEventStore
COPY ./Directory.Build.props ./Directory.Build.props
COPY ./Protos ./Protos

RUN dotnet restore ProjectOrigin.Registry.Server
RUN dotnet build ProjectOrigin.Registry.Server -c Release --no-restore -o /app/build
RUN dotnet publish ProjectOrigin.Registry.Server -c Release -o /app/publish

# ------- production image -------
FROM mcr.microsoft.com/dotnet/aspnet:8.0.1 AS production

ENV USER=dotnetuser
RUN groupadd -r "$USER" && useradd -r -g "$USER" "$USER"

WORKDIR /app
COPY --chown=root:root --from=build /app/publish .
RUN chmod -R 655 .

USER $USER
EXPOSE 5000
ENTRYPOINT ["dotnet", "ProjectOrigin.Registry.Server.dll"]
