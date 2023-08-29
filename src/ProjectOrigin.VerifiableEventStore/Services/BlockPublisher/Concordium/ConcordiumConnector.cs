using System;
using System.Threading.Tasks;
using ConcordiumNetSdk;
using ConcordiumNetSdk.SignKey;
using ConcordiumNetSdk.Transactions;
using Microsoft.Extensions.Options;
using ProjectOrigin.VerifiableEventStore.Services.BlockPublisher;

namespace ProjectOrigin.VerifiableEventStore.Services.BlockchainConnector.Concordium;

public class ConcordiumPublisher : IBlockPublisher, IDisposable
{
    private IOptions<ConcordiumOptions> _options;
    private ConcordiumNodeClient _concordiumNodeClient;
    private TransactionSigner _transactionSigner;

    public ConcordiumPublisher(IOptions<ConcordiumOptions> options)
    {
        _options = options;
        _concordiumNodeClient = new ConcordiumNodeClient(
            new Connection
            {
                Address = options.Value.Address,
                AuthenticationToken = options.Value.AuthenticationToken
            });

        var ed25519TransactionSigner = Ed25519SignKey.From(options.Value.AccountKey);
        _transactionSigner = new TransactionSigner();
        _transactionSigner.AddSignerEntry(ConcordiumNetSdk.Types.Index.Create(0), ConcordiumNetSdk.Types.Index.Create(0), ed25519TransactionSigner);
    }

    public void Dispose() => _concordiumNodeClient.Dispose();

    public Task<ImmutableLog.V1.BlockPublication> PublishBlock(ImmutableLog.V1.BlockHeader blockHeader)
    {
        // TODO: switch to new Concordium library and reimplment this
        throw new NotImplementedException();
    }
}
