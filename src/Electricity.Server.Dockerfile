FROM mcr.microsoft.com/dotnet/aspnet:7.0-alpine AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
RUN apt-get update && apt install build-essential gcc-x86-64-linux-gnu -y
RUN curl --proto '=https' --tlsv1.2 -sSf https://sh.rustup.rs | sh -s -- -y
ENV PATH="$PATH:/root/.cargo/bin"
RUN rustup target add x86_64-unknown-linux-gnu
COPY . .
RUN dotnet restore
RUN dotnet build ProjectOrigin.Electricity.Server -c Release --no-restore -o /app/build

FROM build AS publish
RUN dotnet publish ProjectOrigin.Electricity.Server -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
HEALTHCHECK CMD curl --fail http://localhost:5000/health || exit 1
ENTRYPOINT ["dotnet", "ProjectOrigin.Electricity.Server.dll"]
