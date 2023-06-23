using System;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProjectOrigin.Electricity.Consumption;
using ProjectOrigin.Electricity.Consumption.Verifiers;
using ProjectOrigin.Electricity.Interfaces;
using ProjectOrigin.Electricity.Models;
using ProjectOrigin.Electricity.Production;
using ProjectOrigin.Electricity.Production.Verifiers;
using ProjectOrigin.Electricity.Services;
using ProjectOrigin.Verifier.Utils;
using ProjectOrigin.Verifier.Utils.Interfaces;
using ProjectOrigin.Verifier.Utils.Services;

namespace ProjectOrigin.Electricity.Server;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddGrpc();

        services.AddSingleton<IProtoDeserializer>(new ProtoDeserializer(Assembly.GetAssembly(typeof(V1.ConsumptionIssuedEvent))
            ?? throw new Exception("Could not find assembly")));

        services.AddTransient<IEventVerifier<V1.ConsumptionIssuedEvent>, ConsumptionIssuedVerifier>();
        services.AddTransient<IEventVerifier<ConsumptionCertificate, V1.AllocatedEvent>, ConsumptionAllocatedVerifier>();
        services.AddTransient<IEventVerifier<ConsumptionCertificate, V1.ClaimedEvent>, ConsumptionClaimedVerifier>();
        services.AddTransient<IEventVerifier<ConsumptionCertificate, V1.SlicedEvent>, ConsumptionSlicedVerifier>();

        services.AddTransient<IEventVerifier<V1.ProductionIssuedEvent>, ProductionIssuedVerifier>();
        services.AddTransient<IEventVerifier<ProductionCertificate, V1.AllocatedEvent>, ProductionAllocatedVerifier>();
        services.AddTransient<IEventVerifier<ProductionCertificate, V1.ClaimedEvent>, ProductionClaimedVerifier>();
        services.AddTransient<IEventVerifier<ProductionCertificate, V1.SlicedEvent>, ProductionSlicedVerifier>();
        services.AddTransient<IEventVerifier<ProductionCertificate, V1.TransferredEvent>, ProductionTransferredVerifier>();

        services.AddTransient<IVerifierDispatcher, VerifierDispatcher>();
        services.AddTransient<IRemoteModelLoader, GrpcRemoteModelLoader>();
        services.AddTransient<IModelHydrater, ElectricityModelHydrater>();
        services.AddTransient<IGridAreaIssuerService, GridAreaIssuerOptionsService>();

        services.AddOptions<IssuerOptions>()
            .Configure<IConfiguration>((settings, configuration) =>
            {
                configuration.Bind(settings);
            })
            .Validate((option => option.Verify()))
            .ValidateOnStart();

        services.AddOptions<RegistryOptions>()
            .Configure<IConfiguration>((settings, configuration) =>
            {
                configuration.Bind(settings);
            })
            .Validate(x => x.Verify())
            .ValidateOnStart();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseRouting();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapGrpcService<ElectricityVerifierService>();
            endpoints.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
        });
    }
}
