using System;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProjectOrigin.Registry.Server;
using ProjectOrigin.Registry.Server.Extensions;
using ProjectOrigin.VerifiableEventStore.Services.Repository;

if (args.Contains("--migrate"))
{
    Console.WriteLine("Starting repository migration.");
    var configuration = new ConfigurationBuilder()
        .AddJsonFile("appsettings.json", optional: false)
        .AddEnvironmentVariables()
        .AddCommandLine(args)
        .Build();
    configuration.GetRepositoryUpgrader().Upgrade();
    Console.WriteLine("Repository migrated successfully.");
}

if (args.Contains("--serve"))
{
    Console.WriteLine("Starting server.");

    var builder = WebApplication.CreateBuilder(args);

    var startup = new Startup(builder.Configuration);
    startup.ConfigureServices(builder.Services);

    var app = builder.Build();
    startup.Configure(app, builder.Environment);

    var upgrader = app.Services.GetRequiredService<IRepositoryUpgrader>();
    if (upgrader.IsUpgradeRequired())
        throw new SystemException("Repository is not up to date. Please run with --migrate first.");

    app.Run();
    Console.WriteLine("Server stopped.");
}
