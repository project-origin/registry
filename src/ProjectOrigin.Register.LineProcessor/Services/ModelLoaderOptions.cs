using ProjectOrigin.VerifiableEventStore.Services.EventStore;

namespace ProjectOrigin.Register.LineProcessor.Services;

public record ModelLoaderOptions(Dictionary<string, IEventStore> EventStores);
