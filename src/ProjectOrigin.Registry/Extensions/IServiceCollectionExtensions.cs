using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProjectOrigin.Registry.BlockFinalizer.BlockPublisher;
using ProjectOrigin.Registry.BlockFinalizer.BlockPublisher.Concordium;
using ProjectOrigin.Registry.BlockFinalizer.BlockPublisher.Log;
using ProjectOrigin.Registry.Options;
using ProjectOrigin.Registry.Repository;
using ProjectOrigin.Registry.Repository.InMemory;
using ProjectOrigin.Registry.Repository.Postgres;
using ProjectOrigin.Registry.TransactionStatusCache;
using ProjectOrigin.ServiceCommon.Database;
using ProjectOrigin.ServiceCommon.Database.Postgres;
using ProjectOrigin.ServiceCommon.Extensions;
using Serilog;
using StackExchange.Redis;

namespace ProjectOrigin.Registry.Extensions;

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

    public static void ConfigurePersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var persistence = configuration.GetRequiredSection("PERSISTENCE");
        var type = persistence.GetValue<string>("type")?.ToLower();

        switch (type)
        {
            case "in_memory":
                Log.Warning("Using in memory repository - this is not recommended for production use! Data is not persisted and unable to run multiple instances.");
                services.AddTransient<IDatabaseUpgrader, InMemoryUpgrader>();
                services.AddSingleton<ITransactionRepository, InMemoryRepository>();
                break;

            case "postgresql":
                IEnumerable<Assembly> assemblies = new List<Assembly> { Assembly.GetEntryAssembly()! };
                services.AddSingleton<IDatabaseUpgrader>(serviceProvider => ActivatorUtilities.CreateInstance<DatabaseUpgrader>(serviceProvider, assemblies));
                services.ConfigurePostgres(configuration);
                services.AddTransient<ITransactionRepository, PostgresqlRepository>();
                break;

            default:
                throw new NotSupportedException($"Persistence type ”{type}” not supported");
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
