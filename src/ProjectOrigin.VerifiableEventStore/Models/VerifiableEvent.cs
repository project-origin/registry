namespace ProjectOrigin.VerifiableEventStore.Models;

public record VerifiableEvent(EventId Id, byte[] Content);
