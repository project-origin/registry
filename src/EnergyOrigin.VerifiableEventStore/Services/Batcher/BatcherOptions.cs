namespace EnergyOrigin.VerifiableEventStore.Services.Batcher;

public record BatcherOptions
{
    public long BatchSizeExponent { get; init; }
}
