using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Backplane.StackExchangeRedis;
using ZiggyCreatures.Caching.Fusion.Serialization.SystemTextJson;

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

        var fusionCacheBuilder = services.AddFusionCache()
            .WithOptions(options =>
            {
                options.DistributedCacheCircuitBreakerDuration = TimeSpan.FromSeconds(2);

                options.FailSafeActivationLogLevel = LogLevel.Debug;
                options.SerializationErrorsLogLevel = LogLevel.Warning;
                options.DistributedCacheSyntheticTimeoutsLogLevel = LogLevel.Debug;
                options.DistributedCacheErrorsLogLevel = LogLevel.Error;
                options.FactorySyntheticTimeoutsLogLevel = LogLevel.Debug;
                options.FactoryErrorsLogLevel = LogLevel.Error;
            })
            .WithDefaultEntryOptions(new FusionCacheEntryOptions
            {
                Duration = TimeSpan.FromMinutes(60),

                IsFailSafeEnabled = false,
                AllowStaleOnReadOnly = false,
                FailSafeMaxDuration = TimeSpan.FromHours(2),
                FailSafeThrottleDuration = TimeSpan.FromSeconds(30),

                FactorySoftTimeout = TimeSpan.FromMilliseconds(100),
                FactoryHardTimeout = TimeSpan.FromMilliseconds(1500),

                DistributedCacheSoftTimeout = TimeSpan.FromSeconds(1),
                DistributedCacheHardTimeout = TimeSpan.FromSeconds(2),

                AllowBackgroundDistributedCacheOperations = false,
                AllowBackgroundBackplaneOperations = false,

                JitterMaxDuration = TimeSpan.FromSeconds(2)
            })
            .WithSerializer(new FusionCacheSystemTextJsonSerializer());

        switch (cacheOptions.Type)
        {
            case CacheTypes.InMemory:
                Log.Warning("Using in memory transaction status service - this is not recommended for production use. Mighe result in inconsistent state when multiple instances are running.");
                services.AddDistributedMemoryCache();
                services.AddSingleton<ITransactionStatusService, InMemoryTransactionStatusService>();
                break;

            case CacheTypes.Redis:
                services.AddSingleton<IConnectionMultiplexer>(_ =>
                {
                    var options = new ConfigurationOptions
                    {
                        Password = cacheOptions.Redis!.Password,
                        AbortOnConnectFail = false
                    };

                    foreach (var endpoint in cacheOptions.Redis.Endpoints)
                    {
                        options.EndPoints.Add(endpoint);
                    }

                    return ConnectionMultiplexer.Connect(options);
                });

                fusionCacheBuilder
                    .WithDistributedCache(sp =>
                    {
                        var redis = sp.GetRequiredService<IConnectionMultiplexer>();
                        return new RedisCache(new RedisCacheOptions
                        {
                            ConnectionMultiplexerFactory = () => Task.FromResult(redis)
                        });
                    })
                    .WithBackplane(sp =>
                    {
                        var redis = sp.GetRequiredService<IConnectionMultiplexer>();
                        return new RedisBackplane(new RedisBackplaneOptions
                        {
                            ConnectionMultiplexerFactory = () => Task.FromResult(redis)
                        });
                    });
                break;

            default:
                throw new NotSupportedException($"Cache type ”{cacheOptions.Type}” not supported");
        }

        services.AddTransient<ITransactionStatusService, RedisTransactionStatusService>();
    }
}
