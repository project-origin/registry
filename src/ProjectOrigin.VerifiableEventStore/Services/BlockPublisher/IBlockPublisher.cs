using System.Threading.Tasks;

namespace ProjectOrigin.VerifiableEventStore.Services.BlockPublisher;

public interface IBlockPublisher
{
    Task<ImmutableLog.V1.BlockPublication> PublishBlock(ImmutableLog.V1.BlockHeader blockHeader);
}
