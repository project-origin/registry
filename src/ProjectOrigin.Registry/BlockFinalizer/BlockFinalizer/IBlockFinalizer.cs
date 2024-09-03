using System.Threading;
using System.Threading.Tasks;

namespace ProjectOrigin.VerifiableEventStore.Services.BlockFinalizer;

public interface IBlockFinalizer
{
    Task Execute(CancellationToken stoppingToken);
}
