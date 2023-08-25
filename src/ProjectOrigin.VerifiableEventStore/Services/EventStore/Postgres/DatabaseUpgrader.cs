using System;
using System.Threading;
using DbUp;
using DbUp.Engine;

namespace ProjectOrigin.WalletSystem.Server.Database;

public static class DatabaseUpgrader
{
    private static TimeSpan _sleepTime = TimeSpan.FromSeconds(5);
    private static TimeSpan _defaultTimeout = TimeSpan.FromMinutes(5);

    public static void Upgrade(string? connectionString)
    {
        var upgradeEngine = BuildUpgradeEngine(connectionString);

        TryConnectToDatabaseWithRetry(upgradeEngine);
        var databaseUpgradeResult = upgradeEngine.PerformUpgrade();

        if (!databaseUpgradeResult.Successful)
        {
            throw databaseUpgradeResult.Error;
        }
    }

    public static bool IsUpgradeRequired(string? connectionString)
    {
        var upgradeEngine = BuildUpgradeEngine(connectionString);

        return upgradeEngine.IsUpgradeRequired();
    }

    private static void TryConnectToDatabaseWithRetry(UpgradeEngine upgradeEngine)
    {
        var started = DateTime.UtcNow;
        while (!upgradeEngine.TryConnect(out string msg))
        {
            Console.WriteLine($"Failed to connect to database ({msg}), waiting to retry in {_sleepTime.TotalSeconds} seconds... ");
            Thread.Sleep(_sleepTime);
            if (DateTime.UtcNow - started > _defaultTimeout)
                throw new TimeoutException($"Could not connect to database ({msg}), exceeded retry limit.");
        }
    }

    private static UpgradeEngine BuildUpgradeEngine(string? connectionString)
    {
        return DeployChanges.To
                    .PostgresqlDatabase(connectionString)
                    .WithScriptsEmbeddedInAssembly(typeof(DatabaseUpgrader).Assembly)
                    .LogToAutodetectedLog()
                    .WithExecutionTimeout(TimeSpan.FromMinutes(5))
                    .Build();
    }
}
