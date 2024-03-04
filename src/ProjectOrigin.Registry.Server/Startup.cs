using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using OpenTelemetry.Metrics;

using OpenTelemetry.Trace;

using ProjectOrigin.Registry.Server.Extensions;
using ProjectOrigin.Registry.Server.Grpc;
using ProjectOrigin.Registry.Server.Interfaces;
using ProjectOrigin.Registry.Server.Options;
using ProjectOrigin.Registry.Server.Services;
using ProjectOrigin.VerifiableEventStore.Models;
using ProjectOrigin.VerifiableEventStore.Services.BlockFinalizer;

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
        services.AddSingleton<ITransactionDispatcher, VerifierDispatcher>();

        services.AddOptions<OtlpOptions>()
            .BindConfiguration(OtlpOptions.Prefix)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        var otlpOptions = _configuration.GetSection(OtlpOptions.Prefix).GetValid<OtlpOptions>();
        if (otlpOptions.Enabled)
        {
            services.AddOpenTelemetry()
                .WithMetrics(provider =>
                    provider
                        .AddMeter(BlockFinalizerJob.Meter.Name)
                        .AddMeter(RegistryService.Meter.Name)
                        .AddOtlpExporter(o => o.Endpoint = otlpOptions.Endpoint)
                )
                .WithTracing(provider =>
                    provider
                        .AddGrpcClientInstrumentation(grpcOptions =>
                        {
                            grpcOptions.EnrichWithHttpRequestMessage = (activity, httpRequestMessage) =>
                                activity.SetTag("requestVersion", httpRequestMessage.Version);
                            grpcOptions.EnrichWithHttpResponseMessage = (activity, httpResponseMessage) =>
                                activity.SetTag("responseVersion", httpResponseMessage.Version);
                            grpcOptions.SuppressDownstreamInstrumentation = true;
                        })
                        .AddAspNetCoreInstrumentation()
                        .AddRedisInstrumentation()
                        .AddNpgsql()
                        .AddOtlpExporter(o => o.Endpoint = otlpOptions.Endpoint));
        }



        services.AddOptions<RegistryOptions>().Configure<IConfiguration>((settings, configuration) =>
        {
            configuration.Bind(settings);
        })
        .ValidateDataAnnotations()
        .ValidateOnStart();

        services.AddOptions<VerifierOptions>().Configure<IConfiguration>((settings, configuration) =>
        {
            configuration.Bind(settings);
        })
        .ValidateDataAnnotations()
        .ValidateOnStart();

        services.AddOptions<BlockFinalizationOptions>().Configure<IConfiguration>((settings, configuration) =>
            configuration.GetSection("BlockFinalizer").Bind(settings)
        )
        .ValidateDataAnnotations()
        .ValidateOnStart();

        services.AddOptions<TransactionProcessorOptions>().Configure<IConfiguration>((settings, configuration) =>
            _configuration.GetSection("TransactionProcessor").Bind(settings)
        )
        .ValidateDataAnnotations()
        .ValidateOnStart();

        services.AddOptions<RabbitMqOptions>().Configure<IConfiguration>((settings, configuration) =>
            _configuration.GetSection("RabbitMq").Bind(settings)
        )
        .ValidateDataAnnotations()
        .ValidateOnStart();

        services.ConfigureImmutableLog(_configuration);
        services.ConfigurePersistance(_configuration);
        services.ConfigureTransactionStatusCache(_configuration);

        services.AddSingleton<IQueueResolver, ConsistentHashRingQueueResolver>();
        services.AddSingleton<IRabbitMqChannelPool, RabbitMqChannelPool>();
        services.AddTransient(sp => sp.GetRequiredService<IRabbitMqChannelPool>().GetChannel());
        services.AddTransient<TransactionProcessor>();
        services.AddHttpClient<IRabbitMqHttpClient, RabbitMqHttpClient>();

        services.AddHostedService<TransactionProcessorManager>();
        services.AddHostedService<QueueCleanupService>();

        // Only one server should run the block finalizer
        var serverNumber = _configuration.GetSection("TransactionProcessor").GetValue<int>("ServerNumber");
        if (serverNumber == 0)
        {
            services.AddHostedService<BlockFinalizerBackgroundService>();
            services.AddTransient<IBlockFinalizer, BlockFinalizerJob>();
        }
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
