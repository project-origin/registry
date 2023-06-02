using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProjectOrigin.VerifiableEventStore.Services.BlockchainConnector;
using ProjectOrigin.VerifiableEventStore.Services.BlockchainConnector.Concordium;

static class Configurator
{
    public static void ConfigureImmutableLog(WebApplicationBuilder builder)
    {
        var immutableLogSection = builder.Configuration.GetRequiredSection("ImmutableLog");
        var type = immutableLogSection.GetValue<string>("type")?.ToLower();

        switch (type)
        {
            case "concordium":
                builder.Services.AddOptions<ConcordiumOptions>()
                    .Bind(immutableLogSection.GetRequiredSection("Concordium"))
                    .ValidateDataAnnotations()
                    .ValidateOnStart();
                builder.Services.AddTransient<IBlockchainConnector, ConcordiumConnector>();
                break;

            case "log":
                builder.Services.AddSingleton<IBlockchainConnector, LogBlockchainConnector>();
                break;

            default:
                throw new Exception($"Immutable log type ”{type}” not supported");
        }
    }

}
