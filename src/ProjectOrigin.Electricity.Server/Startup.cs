using Microsoft.Extensions.Options;
using ProjectOrigin.Register.CommandProcessor.Services;
using ProjectOrigin.Register.StepProcessor.Interfaces;
using ProjectOrigin.Register.StepProcessor.Models;
using ProjectOrigin.Register.StepProcessor.Services;
using ProjectOrigin.VerifiableEventStore.Services.BatchProcessor;
using ProjectOrigin.VerifiableEventStore.Services.BlockchainConnector;
using ProjectOrigin.VerifiableEventStore.Services.EventStore;

namespace ProjectOrigin.Electricity.Server;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        VerifierConfiguration.ConfigureServices(services);

        var eventStores = new List<IEventStore>();

        services.AddGrpc();


        services.AddHostedService<BatchProcessorBackgroundService>(sp => new BatchProcessorBackgroundService(sp.GetService<ILogger<BatchProcessorBackgroundService>>()!, eventStores, sp.GetService<IBlockchainConnector>()!));

        // Only for alpha server containing all parts.
        services.AddSingleton<ICommandStepProcessor>((serviceProvider) =>
        {
            var options = serviceProvider.GetService<IOptions<ServerOptions>>()!.Value;

            var eventStoreDictionary = new Dictionary<string, IEventStore>();
            var processors = new Dictionary<string, ICommandStepProcessor>();

            foreach (var reg in options.Registries)
            {
                var eventStore = new MemoryEventStore(Options.Create(reg.Value.VerifiableEventStore));

                eventStores.Add(eventStore);
                eventStoreDictionary.Add(reg.Key, eventStore);
                var fesReg = new InProcessFederatedEventStore(eventStore, eventStoreDictionary);

                var processor = new CommandStepProcessor(Options.Create(new CommandStepProcessorOptions { RegistryName = reg.Key }),
                                                        fesReg,
                                                        serviceProvider.GetService<ICommandStepVerifiere>()!);
                processors.Add(reg.Key, processor);
            }

            return new CommandStepRouter(processors);
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
