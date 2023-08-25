using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProjectOrigin.VerifiableEventStore.Services.BatchPublisher;
using ProjectOrigin.VerifiableEventStore.Services.BatchPublisher.Log;
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
                builder.Services.AddTransient<IBatchPublisher, ConcordiumPublisher>();
                break;

            case "log":
                builder.Services.AddSingleton<IBatchPublisher, LogBatchPublisher>();
                break;

            default:
                throw new Exception($"Immutable log type ”{type}” not supported");
        }
    }

}
