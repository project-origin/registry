using System.Threading.Tasks;

namespace ProjectOrigin.VerifiableEventStore.Services.BlockPublisher;

public interface IBlockPublisher
{
    Task<Registry.V1.BlockPublication> PublishBlock(Registry.V1.BlockHeader blockHeader);
}
