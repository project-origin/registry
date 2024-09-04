using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProjectOrigin.Registry.BlockFinalizer.Process;
using ProjectOrigin.Registry.Extensions;
using ProjectOrigin.Registry.Grpc;
using ProjectOrigin.Registry.MessageBroker;
using ProjectOrigin.Registry.Options;
using ProjectOrigin.Registry.TransactionProcessor;
using ProjectOrigin.ServiceCommon.Extensions;
using ProjectOrigin.ServiceCommon.Grpc;
using ProjectOrigin.ServiceCommon.Otlp;

namespace ProjectOrigin.Registry;

public class Startup
{
    private readonly IConfiguration _configuration;

    public Startup(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.ConfigureDefaultOtlp(_configuration);
        services.ConfigureGrpc(_configuration);
        services.ConfigurePersistance(_configuration);

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
        services.ConfigureTransactionStatusCache(_configuration);

        services.AddSingleton<ITransactionDispatcher, VerifierDispatcher>();
        services.AddSingleton<IQueueResolver, ConsistentHashRingQueueResolver>();
        services.AddSingleton<IRabbitMqChannelPool, RabbitMqChannelPool>();
        services.AddTransient<TransactionProcessorDispatcher>();
        services.AddHttpClient<IRabbitMqHttpClient, RabbitMqHttpClient>();

        services.AddHostedService<TransactionProcessorManager>();
        services.AddHostedService<QueueCleanupService>();

        // Only one server should run the block finalizer
        var processorOptions = _configuration.GetRequiredSection("TransactionProcessor").GetValid<TransactionProcessorOptions>();
        if (processorOptions.ServerNumber == 0)
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
