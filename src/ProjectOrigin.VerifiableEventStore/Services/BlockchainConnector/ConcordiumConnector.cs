using ConcordiumNetSdk;
using ConcordiumNetSdk.SignKey;
using ConcordiumNetSdk.Transactions;
using ConcordiumNetSdk.Types;
using Microsoft.Extensions.Options;

namespace ProjectOrigin.VerifiableEventStore.Services.BlockchainConnector;

public class ConcordiumConnector : IBlockchainConnector, IDisposable
{
    private IOptions<ConcordiumOptions> options;
    private ConcordiumNodeClient concordiumNodeClient;
    private TransactionSigner transactionSigner;

    public ConcordiumConnector(IOptions<ConcordiumOptions> options)
    {
        this.options = options;
        concordiumNodeClient = new ConcordiumNodeClient(
            new Connection
            {
                Address = options.Value.Address,
                AuthenticationToken = options.Value.AuthenticationToken
            });

        var ed25519TransactionSigner = Ed25519SignKey.From(options.Value.AccountKey);
        transactionSigner = new TransactionSigner();
        transactionSigner.AddSignerEntry(ConcordiumNetSdk.Types.Index.Create(0), ConcordiumNetSdk.Types.Index.Create(0), ed25519TransactionSigner);

    }


    public void Dispose() => concordiumNodeClient.Dispose();

    public async Task<Block?> GetBlock(TransactionReference transactionReference)
    {
        var transactionHash = TransactionHash.From(transactionReference.TransactionHash);

        var transactionStatus = await concordiumNodeClient.GetTransactionStatusAsync(transactionHash);

        if (transactionStatus != null
            && transactionStatus.Outcomes != null
            && transactionStatus.Status == TransactionStatusType.Finalized)
        {
            return new Block(transactionStatus.Outcomes.Single().Key, transactionStatus.Status == TransactionStatusType.Finalized);
        }

        return null;
    }

    public async Task<TransactionReference> PublishBytes(byte[] bytes)
    {
        var accountTransactionService = new AccountTransactionService(concordiumNodeClient);

        var address = AccountAddress.From(options.Value.AccountAddress);
        var payload = RegisterDataPayload.Create(bytes);
        var transactionHash = await accountTransactionService.SendAccountTransactionAsync(address, payload, transactionSigner);

        return new TransactionReference(transactionHash.AsString);
    }
}
