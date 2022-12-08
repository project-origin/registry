using Microsoft.Extensions.Options;
using ProjectOrigin.Register.CommandProcessor.Services;
using ProjectOrigin.Register.StepProcessor.Interfaces;
using ProjectOrigin.Register.StepProcessor.Models;
using ProjectOrigin.Register.StepProcessor.Services;
using ProjectOrigin.VerifiableEventStore.Services.Batcher;
using ProjectOrigin.VerifiableEventStore.Services.BlockchainConnector;
using ProjectOrigin.VerifiableEventStore.Services.EventStore;

namespace ProjectOrigin.Electricity.Server;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        var memorystoreRegA = new MemoryEventStore();
        var memorystoreRegB = new MemoryEventStore();

        services.AddGrpc();
        services.AddTransient<ICommandStepVerifier, ElectricityStepVerifier>();
        services.AddTransient<IBlockchainConnector, ConcordiumConnector>();

        services.AddSingleton<ICommandStepProcessor>((serviceProvider) =>
        {
            var eventStoreDictionary = new Dictionary<string, IEventStore>{
                { Registries.RegistryA, memorystoreRegA},
                { Registries.RegistryB, memorystoreRegB}
            };

            var batcher_dk1 = new MemoryBatcher(serviceProvider.GetService<IBlockchainConnector>()!, memorystoreRegA, Options.Create(new BatcherOptions { BatchSizeExponent = 0 }));
            var batcher_dk2 = new MemoryBatcher(serviceProvider.GetService<IBlockchainConnector>()!, memorystoreRegB, Options.Create(new BatcherOptions { BatchSizeExponent = 0 }));

            var processor_dk1 = new CommandStepProcessor(Options.Create(new CommandStepProcessorOptions(Registries.RegistryA, eventStoreDictionary)), serviceProvider.GetService<ICommandStepVerifier>()!, batcher_dk1);
            var processor_dk2 = new CommandStepProcessor(Options.Create(new CommandStepProcessorOptions(Registries.RegistryB, eventStoreDictionary)), serviceProvider.GetService<ICommandStepVerifier>()!, batcher_dk2);

            return new CommandStepRouter(new Dictionary<string, ICommandStepProcessor>(){
                { Registries.RegistryA, processor_dk1 },
                { Registries.RegistryB, processor_dk2 },
            });
        });
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseRouting();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapGrpcService<CommandService>();
            endpoints.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
        });
    }
}
