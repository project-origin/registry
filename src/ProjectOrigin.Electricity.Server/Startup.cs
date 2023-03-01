using Microsoft.Extensions.Options;
using ProjectOrigin.Register.CommandProcessor.Services;
using ProjectOrigin.Register.StepProcessor.Interfaces;
using ProjectOrigin.Register.StepProcessor.Models;
using ProjectOrigin.Register.StepProcessor.Services;
using ProjectOrigin.VerifiableEventStore.Services.Batcher;
using ProjectOrigin.VerifiableEventStore.Services.Batcher.Postgres.Configuration;
using ProjectOrigin.VerifiableEventStore.Services.BlockchainConnector;
using ProjectOrigin.VerifiableEventStore.Services.EventStore;
using ProjectOrigin.VerifiableEventStore.Services.EventStore.Postgres.Configuration;

namespace ProjectOrigin.Electricity.Server;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        VerifierConfiguration.ConfigureServices(services);

        // In-memory setup
        var batchSizeOptions = new BatcherOptions() { BatchSizeExponent = 0 };
        var memorystoreRegA = new MemoryEventStore(batchSizeOptions);
        var memorystoreRegB = new MemoryEventStore(batchSizeOptions);

        // Persistent setup
        // services.AddBatchProcessor();
        // services.AddPostgresEventStore(configuration);

        services.AddGrpc();
        services.AddTransient<IBlockchainConnector, ConcordiumConnector>();
        services.AddSingleton<ICommandStepProcessor>((serviceProvider) =>
        {
            var eventStoreDictionary = new Dictionary<string, IEventStore>{
                { Registries.RegistryA, memorystoreRegA},
                { Registries.RegistryB, memorystoreRegB}
            };

            var batcherRegA = new MemoryBatcher(serviceProvider.GetService<IBlockchainConnector>()!, memorystoreRegA, Options.Create(new BatcherOptions { BatchSizeExponent = 0 }));
            var batcherRegB = new MemoryBatcher(serviceProvider.GetService<IBlockchainConnector>()!, memorystoreRegB, Options.Create(new BatcherOptions { BatchSizeExponent = 0 }));

            var fesRegA = new InProcessFederatedEventStore(batcherRegA, eventStoreDictionary);
            var fesRegB = new InProcessFederatedEventStore(batcherRegB, eventStoreDictionary);

            var processorRegA = new CommandStepProcessor(Options.Create(new CommandStepProcessorOptions(Registries.RegistryA)), fesRegA, serviceProvider.GetService<ICommandStepVerifiere>()!);
            var processorRegB = new CommandStepProcessor(Options.Create(new CommandStepProcessorOptions(Registries.RegistryB)), fesRegB, serviceProvider.GetService<ICommandStepVerifiere>()!);

            return new CommandStepRouter(new Dictionary<string, ICommandStepProcessor>(){
                { Registries.RegistryA, processorRegA },
                { Registries.RegistryB, processorRegB },
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
