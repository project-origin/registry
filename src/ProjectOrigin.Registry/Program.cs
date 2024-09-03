using System;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProjectOrigin.Registry.Server.Exceptions;
using ProjectOrigin.Registry.Server.Extensions;
using ProjectOrigin.VerifiableEventStore.Services.Repository;
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
        await configuration.GetRepositoryUpgrader(Log.Logger).Upgrade();
        Log.Information("Repository migrated successfully.");
    }

    if (args.Contains("--serve"))
    {
        Log.Information("Starting server.");
        WebApplication app = configuration.BuildApp();

        var upgrader = app.Services.GetRequiredService<IRepositoryUpgrader>();
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
