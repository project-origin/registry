using ProjectOrigin.VerifiableEventStore.Services.EventStore;

namespace ProjectOrigin.RequestProcessor.Services;

public record ModelLoaderOptions(Dictionary<string, IEventStore> EventStores);
