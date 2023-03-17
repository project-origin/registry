using Microsoft.Extensions.Options;
using ProjectOrigin.Register.CommandProcessor.Services;
using ProjectOrigin.Register.StepProcessor.Interfaces;
using ProjectOrigin.Register.StepProcessor.Models;
using ProjectOrigin.Register.StepProcessor.Services;
using ProjectOrigin.VerifiableEventStore.Models;
using ProjectOrigin.VerifiableEventStore.Services.BatchProcessor;
using ProjectOrigin.VerifiableEventStore.Services.BlockchainConnector;
using ProjectOrigin.VerifiableEventStore.Services.EventStore;

namespace ProjectOrigin.Electricity.Server;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        VerifierConfiguration.ConfigureServices(services);

        services.AddGrpc();
        services.AddTransient<IBlockchainConnector, ConcordiumConnector>();

        // For single running service with two in memory registers
        services.AddSingleton<ICommandStepProcessor>((serviceProvider) =>
        {
            var eventStoreOptions = serviceProvider.GetService<IOptions<VerifiableEventStoreOptions>>()!;

            var memorystoreRegA = new MemoryEventStore(eventStoreOptions);
            var memorystoreRegB = new MemoryEventStore(eventStoreOptions);

            var eventStoreDictionary = new Dictionary<string, IEventStore>{
                { Registries.RegistryA, memorystoreRegA},
                { Registries.RegistryB, memorystoreRegB}
            };

            services.AddHostedService<BatchProcessorBackgroundService>(sp => new BatchProcessorBackgroundService(sp.GetService<ILogger<BatchProcessorBackgroundService>>()!, memorystoreRegA, sp.GetService<IBlockchainConnector>()!));
            services.AddHostedService<BatchProcessorBackgroundService>(sp => new BatchProcessorBackgroundService(sp.GetService<ILogger<BatchProcessorBackgroundService>>()!, memorystoreRegB, sp.GetService<IBlockchainConnector>()!));

            var fesRegA = new InProcessFederatedEventStore(memorystoreRegA, eventStoreDictionary);
            var fesRegB = new InProcessFederatedEventStore(memorystoreRegB, eventStoreDictionary);

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
