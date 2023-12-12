using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using ProjectOrigin.ImmutableLog.V1;
using ProjectOrigin.VerifiableEventStore.Services.BlockPublisher;
using Concordium.Sdk.Client;
using Concordium.Sdk.Crypto;
using Concordium.Sdk.Types;
using Concordium.Sdk.Transactions;
using ConcordiumV2 = Concordium.Grpc.V2;
using Microsoft.Extensions.Logging;
using Google.Protobuf;

namespace ProjectOrigin.VerifiableEventStore.Services.BlockchainConnector.Concordium;

public class ConcordiumPublisher : IBlockPublisher, IDisposable
{
    private readonly TimeSpan sleepTime = TimeSpan.FromSeconds(15);
    private readonly ILogger<ConcordiumPublisher> _logger;
    private readonly IOptions<ConcordiumOptions> _options;
    private readonly ConcordiumClient _concordiumClient;
    private bool _disposed = false;

    public ConcordiumPublisher(ILogger<ConcordiumPublisher> logger, IOptions<ConcordiumOptions> options)
    {
        _logger = logger;
        _options = options;
        _concordiumClient = new ConcordiumClient(
            new Uri(options.Value.Address), new ConcordiumClientOptions
            {
                Timeout = TimeSpan.FromSeconds(60),
            }
            );

        GetSigner(); // Validate key
    }

    public async Task<BlockPublication> PublishBlock(BlockHeader blockHeader)
    {
        var data = blockHeader.ToByteArray();
        var request = new RegisterData(OnChainData.From(data));
        var hash = await SignAndSendTransaction(request);
        var blockSummary = await AwaitFinalized(hash);

        return new BlockPublication
        {
            Concordium = new BlockPublication.Types.Concordium
            {
                TransactionHash = hash.ToProto().Value,
                BlockHash = blockSummary.BlockHash.Value
            }
        };
    }

    private async Task<ConcordiumV2.BlockItemSummaryInBlock> AwaitFinalized(TransactionHash hash)
    {
        var protoHash = hash.ToProto();

        while (true)
        {
            await Task.Delay(sleepTime);
            var status = await _concordiumClient.Raw.GetBlockItemStatusAsync(protoHash);
            switch (status.StatusCase)
            {

                case ConcordiumV2.BlockItemStatus.StatusOneofCase.Finalized:
                    _logger.LogDebug("Block finalized");
                    return status.Finalized.Outcome;

                case ConcordiumV2.BlockItemStatus.StatusOneofCase.Received:
                case ConcordiumV2.BlockItemStatus.StatusOneofCase.Committed:
                    _logger.LogDebug("Block not yet finalized");
                    break;

                case ConcordiumV2.BlockItemStatus.StatusOneofCase.None:
                    throw new NotSupportedException("Transaction status is None");

                default:
                    throw new NotImplementedException($"Transaction status is {status.StatusCase}");
            }
        }
    }

    private async Task<TransactionHash> SignAndSendTransaction(AccountTransactionPayload transaction)
    {
        var sender = AccountAddress.From(_options.Value.AccountAddress);
        var sequenceNumber = (await _concordiumClient.GetNextAccountSequenceNumberAsync(sender)).Item1;
        var expire = Expiry.AtMinutesFromNow(10);
        var signer = GetSigner();

        var preparedTransaction = transaction.Prepare(sender, sequenceNumber, expire);
        var signedTransaction = preparedTransaction.Sign(signer);

        return await _concordiumClient.SendAccountTransactionAsync(signedTransaction);
    }

    private TransactionSigner GetSigner()
    {
        var ed25519TransactionSigner = Ed25519SignKey.From(_options.Value.AccountKey);
        var signer = new TransactionSigner();
        signer.AddSignerEntry(new AccountCredentialIndex(0), new AccountKeyIndex(0), ed25519TransactionSigner);
        return signer;
    }


    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _concordiumClient.Dispose();
            }
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~ConcordiumPublisher()
    {
        Dispose(false);
    }

}
