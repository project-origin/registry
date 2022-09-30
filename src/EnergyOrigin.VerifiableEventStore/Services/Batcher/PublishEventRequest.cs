namespace EnergyOrigin.VerifiableEventStore.Services.Batcher;

public record PublishEventRequest(Guid EventId, byte[] EventData);
