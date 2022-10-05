namespace EnergyOrigin.VerifiableEventStore.Models;

public record EventId(Guid EventStreamId, int index);
