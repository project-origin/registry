using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProjectOrigin.VerifiableEventStore.Services.Repository;

namespace ProjectOrigin.Registry.Server.Extensions;

public static class IConfigurationExtensions
{
    public static IRepositoryUpgrader GetRepositoryUpgrader(this IConfiguration configuration)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.ConfigurePersistance(configuration);
        using var serviceProvider = services.BuildServiceProvider();
        return serviceProvider.GetRequiredService<IRepositoryUpgrader>();
    }
}
