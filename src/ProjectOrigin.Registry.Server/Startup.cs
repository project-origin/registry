using MassTransit;
using MassTransit.Monitoring;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using ProjectOrigin.Registry.Server.Interfaces;
using ProjectOrigin.Registry.Server.Models;
using ProjectOrigin.Registry.Server.Services;
using ProjectOrigin.VerifiableEventStore.Models;
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

        services.AddOpenTelemetry()
            .WithMetrics(provider =>
                provider
                    .AddMeter(InstrumentationOptions.MeterName)
                    .AddMeter(BatchProcessorJob.Meter.Name)
                    .AddMeter(RegistryService.Meter.Name)
                    .AddPrometheusExporter()
            );

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

        services.AddOptions<VerifiableEventStoreOptions>().Configure<IConfiguration>((settings, configuration) =>
        {
            configuration.GetSection("VerifiableEventStore").Bind(settings);
        });

        // Memory only section
        services.AddDistributedMemoryCache();
        services.AddTransient<IBlockchainConnector, LogBlockchainConnector>();
        services.AddSingleton<IEventStore, MemoryEventStore>();
        services.AddMassTransit(x =>
        {
            x.AddConsumer<TransactionProcessor>(cfg =>
            {
                cfg.Options<JobOptions<TransactionJob>>(options => options
                    // Currently set to 1 as to ensure transactions on the same certificate are not processed in parallel.
                    // This will be solved in the future by using methods like RabbitMQ consistent hash exchange.
                    .SetConcurrentJobLimit(1));
            });

            x.SetKebabCaseEndpointNameFormatter();
            x.UsingInMemory((context, cfg) =>
            {
                cfg.UseDelayedMessageScheduler();
                cfg.ServiceInstance(instance =>
                {
                    instance.ConfigureJobServiceEndpoints();
                    instance.ConfigureEndpoints(context);
                });
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

        app.UseOpenTelemetryPrometheusScrapingEndpoint();
    }
}
