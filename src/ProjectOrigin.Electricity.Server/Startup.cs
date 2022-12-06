using Microsoft.Extensions.Options;
using ProjectOrigin.Register.CommandProcessor.Interfaces;
using ProjectOrigin.Register.CommandProcessor.Services;
using ProjectOrigin.Register.StepProcessor.Interfaces;
using ProjectOrigin.Register.StepProcessor.Models;
using ProjectOrigin.Register.StepProcessor.Services;
using ProjectOrigin.Register.Utils.Interfaces;
using ProjectOrigin.Register.Utils.Services;
using ProjectOrigin.VerifiableEventStore.Services.Batcher;
using ProjectOrigin.VerifiableEventStore.Services.BlockchainConnector;
using ves = ProjectOrigin.VerifiableEventStore.Services;

namespace ProjectOrigin.Electricity.Server;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        var batcherOptions = new BatcherOptions() { BatchSizeExponent = 0 };
        var memorystore_dk1 = new ves.EventStore.MemoryEventStore(batcherOptions);
        var memorystore_dk2 = new ves.EventStore.MemoryEventStore(batcherOptions);

        services.AddGrpc();
        services.AddSingleton<IProtoSerializer>(new ProtoSerializer(typeof(V1.ClaimCommand).Assembly));
        services.AddTransient<ICommandStepDispatcher, CommandStepDispatcher>();
        services.AddTransient<ICommandStepVerifierFactory, ElectricityCommandStepVerifierFactory>();
        services.AddTransient<IBlockchainConnector, ConcordiumConnector>();

        services.AddTransient<IModelLoader>((serviceProvider) =>
            new ModelLoader(Options.Create(new ModelLoaderOptions(new Dictionary<string, ves.EventStore.IEventStore>()
            {
                { Registries.RegistryA, memorystore_dk1 },
                {  Registries.RegistryB, memorystore_dk2 },
            }))));

        services.AddSingleton<ICommandStepProcessor>((serviceProvider) =>
        {
            var batcher_dk1 = new MemoryBatcher(serviceProvider.GetService<IBlockchainConnector>()!, memorystore_dk1, Options.Create(new BatcherOptions { BatchSizeExponent = 0 }));
            var batcher_dk2 = new MemoryBatcher(serviceProvider.GetService<IBlockchainConnector>()!, memorystore_dk2, Options.Create(new BatcherOptions { BatchSizeExponent = 0 }));

            var processor_dk1 = new SynchronousCommandStepProcessor(Options.Create(new CommandStepProcessorOptions(Registries.RegistryA)), serviceProvider.GetService<ICommandStepDispatcher>()!, batcher_dk1);
            var processor_dk2 = new SynchronousCommandStepProcessor(Options.Create(new CommandStepProcessorOptions(Registries.RegistryB)), serviceProvider.GetService<ICommandStepDispatcher>()!, batcher_dk2);

            return new CommandStepRouter(new Dictionary<string, ICommandStepProcessor>(){
                { Registries.RegistryA, processor_dk1 },
                { Registries.RegistryB, processor_dk2 },
            });
        });

        services.AddTransient<ICommandDispatcher>((serviceProvider) =>
        {
            var orchestrator = ActivatorUtilities.CreateInstance<CommandOrchestrator>(serviceProvider);
            return new CommandDispatcher(new List<object>() { orchestrator });
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
