namespace EnergyOrigin.VerifiableEventStore.Api.Services.Batcher;

public record BatcherOptions
{
    public long BatchSize { get; init; }
}
