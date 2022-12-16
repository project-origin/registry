using ProjectOrigin.VerifiableEventStore.Models;

namespace ProjectOrigin.Register.StepProcessor.Interfaces;

public interface IFederatedEventStore
{
    Task PublishEvent(VerifiableEvent e);
    Task<IDictionary<V1.FederatedStreamId, IEnumerable<V1.SignedEvent>>> GetStreams(IEnumerable<V1.FederatedStreamId> streamsIds);
}
