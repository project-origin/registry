using System.Threading.Tasks;

namespace ProjectOrigin.VerifiableEventStore.Services.BatchPublisher;

public interface IBatchPublisher
{
    Task<ImmutableLog.V1.BlockPublication> PublishBatch(ImmutableLog.V1.BlockHeader batchHeader);
}
