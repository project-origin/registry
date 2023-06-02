using System.Reflection;
using ProjectOrigin.Electricity.Consumption;
using ProjectOrigin.Electricity.Consumption.Verifiers;
using ProjectOrigin.Electricity.Interfaces;
using ProjectOrigin.Electricity.Production;
using ProjectOrigin.Electricity.Production.Verifiers;
using ProjectOrigin.Electricity.Services;
using ProjectOrigin.Registry.Utils;
using ProjectOrigin.Registry.Utils.Interfaces;
using ProjectOrigin.Registry.Utils.Services;
using ProjectOrigin.WalletSystem.Server.HDWallet;

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
        services.AddTransient<IKeyAlgorithm, Secp256k1Algorithm>();
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
