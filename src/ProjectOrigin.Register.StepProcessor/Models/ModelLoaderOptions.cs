using ProjectOrigin.VerifiableEventStore.Services.EventStore;

namespace ProjectOrigin.Register.StepProcessor.Models;

public record ModelLoaderOptions(Dictionary<string, IEventStore> EventStores);
