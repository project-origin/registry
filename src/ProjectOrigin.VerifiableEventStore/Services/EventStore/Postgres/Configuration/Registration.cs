using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ProjectOrigin.VerifiableEventStore.Services.EventStore.Postgres.Configuration;

public static class Registration
{
    public static IServiceCollection AddPostgresEventStore(IServiceCollection services, IConfiguration configuration)
    {
        var options = new PostgresqlEventStoreOptions
        {
            BatchExponent = 10,
            CreateSchema = true,
            ConnectionString = configuration.GetConnectionString("EventStore") ?? string.Empty
        };
        services.AddSingleton(options);
        services.TryAddSingleton<IEventStore, PostgresqlEventStore>();

        return services;
    }
}
