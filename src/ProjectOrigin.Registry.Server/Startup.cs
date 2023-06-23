using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProjectOrigin.Registry.Server.Interfaces;
using ProjectOrigin.Registry.Server.Models;
using ProjectOrigin.Registry.Server.Services;
using ProjectOrigin.VerifiableEventStore.Services.BatchProcessor;
using ProjectOrigin.VerifiableEventStore.Services.BlockchainConnector;
using ProjectOrigin.VerifiableEventStore.Services.EventStore;
using ProjectOrigin.VerifiableEventStore.Services.TransactionStatusCache;

namespace ProjectOrigin.Registry.Server;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddGrpc();
        services.AddHostedService<BatchProcessorBackgroundService>();
        services.AddTransient<ITransactionStatusService, TransactionStatusService>();
        services.AddSingleton<ITransactionDispatcher, TransactionDispatcher>();

        services.AddOptions<TransactionProcessorOptions>().Configure<IConfiguration>((settings, configuration) =>
        {
            configuration.Bind(settings);
        })
        .ValidateDataAnnotations()
        .ValidateOnStart();

        services.AddOptions<VerifierOptions>().Configure<IConfiguration>((settings, configuration) =>
        {
            configuration.Bind(settings);
        });

        // Memory only section
        services.AddDistributedMemoryCache();
        services.AddTransient<IBlockchainConnector, LogBlockchainConnector>();
        services.AddSingleton<IEventStore, MemoryEventStore>();
        services.AddMassTransit(x =>
        {
            x.AddConsumer<TransactionProcessor>();
            x.AddSagaRepository<JobSaga>().InMemoryRepository();
            x.AddSagaRepository<JobTypeSaga>().InMemoryRepository();
            x.AddSagaRepository<JobAttemptSaga>().InMemoryRepository();

            x.SetKebabCaseEndpointNameFormatter();
            x.UsingInMemory((context, cfg) =>
            {
                cfg.UseDelayedMessageScheduler();

                var options = new ServiceInstanceOptions()
                    .SetEndpointNameFormatter(context.GetService<IEndpointNameFormatter>() ?? KebabCaseEndpointNameFormatter.Instance);

                cfg.ServiceInstance(options, instance =>
                {
                    instance.ConfigureJobServiceEndpoints(js =>
                    {
                        js.SagaPartitionCount = 1;
                        js.FinalizeCompleted = false; // for demo purposes, to get state

                        js.ConfigureSagaRepositories(context);
                    });

                    // configure the job consumer on the job service endpoints
                    instance.ConfigureEndpoints(context, f => f.Include<TransactionProcessor>());
                });

                // Configure the remaining consumers
                cfg.ConfigureEndpoints(context);
            });
        });

    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseRouting();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapGrpcService<RegistryService>();
            endpoints.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
        });
    }
}
