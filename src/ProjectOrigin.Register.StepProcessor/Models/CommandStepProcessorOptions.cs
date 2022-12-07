using ProjectOrigin.VerifiableEventStore.Services.EventStore;

namespace ProjectOrigin.Register.StepProcessor.Models;

public record CommandStepProcessorOptions(string RegistryName, Dictionary<string, IEventStore> EventStores);
