using System.Threading.Tasks;

namespace ProjectOrigin.Registry.BlockFinalizer.BlockPublisher;

public interface IBlockPublisher
{
    Task<Registry.V1.BlockPublication> PublishBlock(Registry.V1.BlockHeader blockHeader);
}
