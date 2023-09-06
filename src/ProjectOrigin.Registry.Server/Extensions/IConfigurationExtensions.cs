using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProjectOrigin.VerifiableEventStore.Services.Repository;
using Serilog;
using Serilog.Enrichers.Span;
using Serilog.Formatting.Json;

namespace ProjectOrigin.Registry.Server.Extensions;

public static class IConfigurationExtensions
{
    public static IRepositoryUpgrader GetRepositoryUpgrader(this IConfiguration configuration, Serilog.ILogger logger)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSerilog(logger);
        services.ConfigurePersistance(configuration);
        using var serviceProvider = services.BuildServiceProvider();
        return serviceProvider.GetRequiredService<IRepositoryUpgrader>();
    }

    public static WebApplication BuildApp(this IConfigurationRoot configuration)
    {
        var builder = WebApplication.CreateBuilder();

        builder.Configuration.Sources.Clear();
        builder.Configuration.AddConfiguration(configuration, shouldDisposeConfiguration: true);

        builder.Logging.ClearProviders();
        builder.Logging.AddSerilog(Log.Logger);

        var startup = new Startup(builder.Configuration);
        startup.ConfigureServices(builder.Services);

        var app = builder.Build();
        startup.Configure(app, builder.Environment);
        return app;
    }

    public static Serilog.ILogger GetSeriLogger(this IConfiguration configuration)
    {
        var loggerConfiguration = new LoggerConfiguration()
            .Filter.ByExcluding("RequestPath like '/health%'")
            .Filter.ByExcluding("RequestPath like '/metrics%'")
            .Enrich.WithSpan();

        switch (configuration.GetValue<string>("LogOutputFormat"))
        {
            case "json":
                loggerConfiguration = loggerConfiguration.WriteTo.Console(new JsonFormatter());
                break;

            case "text":
                loggerConfiguration = loggerConfiguration.WriteTo.Console();
                break;

            default:
                throw new ArgumentOutOfRangeException("LogOutputFormat", "Invalid log output format.");
        }

        return loggerConfiguration.CreateLogger();
    }
}
