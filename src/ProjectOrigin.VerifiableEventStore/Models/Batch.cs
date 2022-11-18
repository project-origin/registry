namespace ProjectOrigin.VerifiableEventStore.Models;

public record Batch(string BlockId, string TransactionId, List<VerifiableEvent> Events);
