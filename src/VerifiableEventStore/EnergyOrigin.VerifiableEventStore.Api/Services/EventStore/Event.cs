namespace EnergyOrigin.VerifiableEventStore.Api.Services.EventStore;

public record Event(Guid Id, Byte[] Content);
