namespace EnergyOrigin.VerifiableEventStore.Services.EventStore;

public record Event(Guid Id, Byte[] Content);
