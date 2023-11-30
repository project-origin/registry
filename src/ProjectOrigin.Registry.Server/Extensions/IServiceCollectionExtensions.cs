using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ProjectOrigin.Registry.Server.Models;
using ProjectOrigin.VerifiableEventStore.Services.BlockchainConnector.Concordium;
using ProjectOrigin.VerifiableEventStore.Services.BlockPublisher;
using ProjectOrigin.VerifiableEventStore.Services.BlockPublisher.Log;
using ProjectOrigin.VerifiableEventStore.Services.EventStore.InMemory;
using ProjectOrigin.VerifiableEventStore.Services.EventStore.Postgres;
using ProjectOrigin.VerifiableEventStore.Services.Repository;
using ProjectOrigin.VerifiableEventStore.Services.TransactionStatusCache;
using Serilog;
using StackExchange.Redis;

namespace ProjectOrigin.Registry.Server.Extensions;

public static class IServiceCollectionExtensions
{
    public static void ConfigureImmutableLog(this IServiceCollection services, IConfiguration configuration)
    {
        var immutableLogSection = configuration.GetRequiredSection("ImmutableLog");
        var type = immutableLogSection.GetValue<string>("type")?.ToLower();

        switch (type)
        {
            case "concordium":
                services.AddOptions<ConcordiumOptions>()
                    .Bind(immutableLogSection.GetRequiredSection("Concordium"))
                    .ValidateDataAnnotations()
                    .ValidateOnStart();
                services.AddTransient<IBlockPublisher, ConcordiumPublisher>();
                break;

            case "log":
                services.AddTransient<IBlockPublisher, LogPublisher>();
                break;

            default:
                throw new NotSupportedException($"Immutable log type ”{type}” not supported");
        }
    }

    public static void ConfigurePersistance(this IServiceCollection services, IConfiguration configuration)
    {
        var persistance = configuration.GetRequiredSection("PERSISTANCE");
        var type = persistance.GetValue<string>("type")?.ToLower();

        switch (type)
        {
            case "in_memory":
                services.AddTransient<IRepositoryUpgrader, InMemoryUpgrader>();
                services.AddSingleton<ITransactionRepository, InMemoryRepository>();
                break;

            case "postgresql":
                services.AddTransient<IRepositoryUpgrader, PostgresqlUpgrader>();
                services.AddTransient<ITransactionRepository, PostgresqlRepository>();
                services.AddOptions<PostgresqlEventStoreOptions>()
                    .Bind(persistance.GetRequiredSection("postgresql"))
                    .ValidateDataAnnotations()
                    .ValidateOnStart();
                break;

            default:
                throw new NotSupportedException($"Persistance type ”{type}” not supported");
        }
    }

    public static void ConfigureTransactionStatusCache(this IServiceCollection services, IConfiguration configuration)
    {
        var cacheOptions = configuration.GetSection("cache").GetValid<CacheOptions>();

        switch (cacheOptions.Type)
        {
            case CacheTypes.InMemory:
                Log.Warning("Using in memory transaction status service - this is not recommended for production use. Mighe result in inconsistent state when multiple instances are running.");
                services.AddDistributedMemoryCache();
                services.AddSingleton<ITransactionStatusService, InMemoryTransactionStatusService>();
                break;

            case CacheTypes.Redis:
                services.AddSingleton<IConnectionMultiplexer>(services =>
                {
                    ConfigurationOptions redisOptions = new ConfigurationOptions()
                    {
                        Password = cacheOptions.Redis!.Password,
                        EndPoints = { cacheOptions.Redis!.ConnectionString }
                    };

                    var connection = ConnectionMultiplexer.Connect(redisOptions);
                    return connection;
                });
                services.AddTransient<ITransactionStatusService, RedisTransactionStatusService>();
                break;

            default:
                throw new NotSupportedException($"Cache type ”{cacheOptions.Type}” not supported");
        }
    }
}
