namespace EnergyOrigin.VerifiableEventStore.Api.Services.EventStore;

public record Batch(string blockId, string transactionId, List<Event> Events);
