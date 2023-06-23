using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ProjectOrigin.VerifiableEventStore.Services.EventStore.Postgres.Configuration;

public static class Registration
{
    public static IServiceCollection AddPostgresEventStore(this IServiceCollection services, IConfiguration configuration)
    {
        var options = new PostgresqlEventStoreOptions
        {
            BatchExponent = 10,
            CreateSchema = true,
            ConnectionString = configuration.GetConnectionString("EventStore") ?? string.Empty
        };
        services.AddSingleton(options);
        throw new NotSupportedException();
        // services.TryAddSingleton<IEventStore, PostgresqlEventStore>();
        //return services;
    }
}
