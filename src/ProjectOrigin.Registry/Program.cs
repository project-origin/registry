using System;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProjectOrigin.Registry;
using ProjectOrigin.Registry.Exceptions;
using ProjectOrigin.Registry.Extensions;
using ProjectOrigin.ServiceCommon.Database;
using ProjectOrigin.ServiceCommon.Extensions;
using ProjectOrigin.ServiceCommon.Logging;
using Serilog;

var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false)
    .AddJsonFile($"appsettings.{environment}.json", optional: true)
    .AddEnvironmentVariables()
    .AddCommandLine(args)
    .Build();

Log.Logger = configuration.GetSeriLogger();

try
{
    if (args.Contains("--migrate"))
    {
        Log.Information("Starting repository migration.");
        await configuration.GetDatabaseUpgrader(Log.Logger).Upgrade();
        Log.Information("Repository migrated successfully.");
    }

    if (args.Contains("--serve"))
    {
        Log.Information("Starting server.");
        WebApplication app = configuration.BuildApp<Startup>();

        var upgrader = app.Services.GetRequiredService<IDatabaseUpgrader>();
        if (await upgrader.IsUpgradeRequired())
            throw new DatabaseStateException("Repository is not up to date. Please run with --migrate first.");

        await app.RunAsync();
        Log.Information("Server stopped.");
    }
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
    Environment.ExitCode = -1;
}
finally
{
    Log.CloseAndFlush();
}
