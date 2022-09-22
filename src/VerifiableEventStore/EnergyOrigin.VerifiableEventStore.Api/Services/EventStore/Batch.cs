namespace EnergyOrigin.VerifiableEventStore.Api.Services.EventStore;

public record Batch(string BlockId, string TransactionId, List<Event> Events);
