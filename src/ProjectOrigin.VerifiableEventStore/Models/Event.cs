namespace ProjectOrigin.VerifiableEventStore.Models;

public record Event(EventId Id, byte[] Content);
