namespace ProjectOrigin.VerifiableEventStore.Models;

public record EventId(Guid EventStreamId, int Index);
