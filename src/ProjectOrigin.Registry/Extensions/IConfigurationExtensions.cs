using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProjectOrigin.ServiceCommon.Database;
using Serilog;

namespace ProjectOrigin.Registry.Extensions;

public static class IConfigurationExtensions
{
    public static IDatabaseUpgrader GetDatabaseUpgrader(this IConfiguration configuration, ILogger logger)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSerilog(logger);
        services.ConfigurePersistence(configuration);
        using var serviceProvider = services.BuildServiceProvider();
        return serviceProvider.GetRequiredService<IDatabaseUpgrader>();
    }
}
