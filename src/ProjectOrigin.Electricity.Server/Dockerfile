FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM rust:buster AS rust-builder
WORKDIR /src
COPY ./rust-ffi .
RUN cargo build --release

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS dotnet-build
WORKDIR /src
COPY . .
RUN dotnet restore
RUN dotnet build ProjectOrigin.Electricity.Server -c Release --no-restore -o /app/build

FROM dotnet-build AS dotnet-publish
RUN dotnet publish ProjectOrigin.Electricity.Server -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=dotnet-publish /app/publish .
COPY --from=rust-builder /src/target/release/librust_ffi.so .
HEALTHCHECK CMD curl --fail http://localhost:5000/health || exit 1
ENTRYPOINT ["dotnet", "ProjectOrigin.Electricity.Server.dll"]
