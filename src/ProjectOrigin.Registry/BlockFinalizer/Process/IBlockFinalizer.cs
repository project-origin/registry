using System.Threading;
using System.Threading.Tasks;

namespace ProjectOrigin.Registry.BlockFinalizer.Process;

public interface IBlockFinalizer
{
    Task Execute(CancellationToken stoppingToken);
}
