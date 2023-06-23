namespace ProjectOrigin.VerifiableEventStore.Models;

public record VerifiableEvent(EventId Id, string TransactionId, byte[] Content);
