using System;
using System.Threading;
using DbUp;
using DbUp.Engine;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProjectOrigin.VerifiableEventStore.Services.Repository;

namespace ProjectOrigin.VerifiableEventStore.Services.EventStore.Postgres;

public class PostgresqlUpgrader : IRepositoryUpgrader
{
    private static TimeSpan _sleepTime = TimeSpan.FromSeconds(5);
    private static TimeSpan _defaultTimeout = TimeSpan.FromMinutes(5);
    private readonly ILogger<PostgresqlUpgrader> _logger;
    private readonly string _connectionString;

    public PostgresqlUpgrader(ILogger<PostgresqlUpgrader> logger, IOptions<PostgresqlEventStoreOptions> options)
    {
        _logger = logger;
        _connectionString = options.Value.ConnectionString;
    }

    public bool IsUpgradeRequired()
    {
        var upgradeEngine = BuildUpgradeEngine(_connectionString);

        return upgradeEngine.IsUpgradeRequired();
    }

    public void Upgrade()
    {
        var upgradeEngine = BuildUpgradeEngine(_connectionString);

        TryConnectToDatabaseWithRetry(upgradeEngine);
        var databaseUpgradeResult = upgradeEngine.PerformUpgrade();

        if (!databaseUpgradeResult.Successful)
        {
            throw databaseUpgradeResult.Error;
        }
    }

    private void TryConnectToDatabaseWithRetry(UpgradeEngine upgradeEngine)
    {
        var started = DateTime.UtcNow;
        while (!upgradeEngine.TryConnect(out string msg))
        {
            _logger.LogWarning($"Failed to connect to database ({msg}), waiting to retry in {_sleepTime.TotalSeconds} seconds... ");
            Thread.Sleep(_sleepTime);
            if (DateTime.UtcNow - started > _defaultTimeout)
                throw new TimeoutException($"Could not connect to database ({msg}), exceeded retry limit.");
        }
    }

    private UpgradeEngine BuildUpgradeEngine(string? connectionString)
    {
        return DeployChanges.To
                    .PostgresqlDatabase(connectionString)
                    .WithScriptsEmbeddedInAssembly(typeof(PostgresqlUpgrader).Assembly)
                    .LogToAutodetectedLog()
                    .WithExecutionTimeout(TimeSpan.FromMinutes(5))
                    .Build();
    }
}
