using ProjectOrigin.VerifiableEventStore.Services.EventStore;

namespace ProjectOrigin.Register.LineProcessor.Models;

public record ModelLoaderOptions(Dictionary<string, IEventStore> EventStores);
