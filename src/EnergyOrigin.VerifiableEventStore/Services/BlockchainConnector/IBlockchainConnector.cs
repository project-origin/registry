using System.Threading.Tasks;

namespace EnergyOrigin.VerifiableEventStore.Services.BlockchainConnector;

public interface IBlockchainConnector
{
    Task<TransactionReference> PublishBytes(byte[] bytes);

    Task<Block?> GetBlock(TransactionReference transactionId);
}
