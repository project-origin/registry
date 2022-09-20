namespace EnergyOrigin.VerifiableEventStore.Api.Shared.BlochainConnector;

public interface IBlockchainConnector
{
    TransactionReference PublishBytes(byte[] bytes);

    Block? GetBlock(TransactionReference transactionId);
}
