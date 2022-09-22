namespace EnergyOrigin.VerifiableEventStore.Api.Services.Batcher;

public record BatcherOptions
{
    public long BatchSizeExponent { get; init; }
}
