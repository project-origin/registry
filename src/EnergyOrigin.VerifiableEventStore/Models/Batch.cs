namespace EnergyOrigin.VerifiableEventStore.Models;

public record Batch(string BlockId, string TransactionId, List<Event> Events);
