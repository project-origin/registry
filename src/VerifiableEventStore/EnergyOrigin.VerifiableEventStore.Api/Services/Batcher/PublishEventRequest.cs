namespace EnergyOrigin.VerifiableEventStore.Api.Services.Batcher;

public record PublishEventRequest(Guid EventId, byte[] EventData);
