using MassTransit;
using MassTransit.Monitoring;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using ProjectOrigin.Registry.Server.Extensions;
using ProjectOrigin.Registry.Server.Interfaces;
using ProjectOrigin.Registry.Server.Models;
using ProjectOrigin.Registry.Server.Services;
using ProjectOrigin.VerifiableEventStore.Models;
using ProjectOrigin.VerifiableEventStore.Services.BlockFinalizer;
using ProjectOrigin.VerifiableEventStore.Services.TransactionStatusCache;

namespace ProjectOrigin.Registry.Server;

public class Startup
{
    private readonly IConfiguration _configuration;

    public Startup(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddGrpc();
        services.AddHostedService<BlockFinalizerBackgroundService>();
        services.AddTransient<ITransactionStatusService, TransactionStatusService>();
        services.AddSingleton<ITransactionDispatcher, TransactionDispatcher>();

        services.AddOpenTelemetry()
            .WithMetrics(provider =>
                provider
                    .AddMeter(InstrumentationOptions.MeterName)
                    .AddMeter(BlockFinalizerJob.Meter.Name)
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

        services.AddOptions<BlockFinalizationOptions>().Configure<IConfiguration>((settings, configuration) =>
        {
            configuration.GetSection("BlockFinalizer").Bind(settings);
        });

        services.ConfigureImmutableLog(_configuration);
        services.ConfigurePersistance(_configuration);

        // Memory only section
        services.AddDistributedMemoryCache();
        services.AddMassTransit(x =>
        {
            x.AddConsumer<VerifyTransactionConsumer, VerifyTransactionConsumerDefinition>();

            x.SetKebabCaseEndpointNameFormatter();
            x.UsingInMemory((context, cfg) =>
            {
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

        app.UseOpenTelemetryPrometheusScrapingEndpoint();
    }

}
