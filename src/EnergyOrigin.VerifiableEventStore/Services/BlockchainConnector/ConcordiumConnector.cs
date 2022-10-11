using ConcordiumNetSdk;
using ConcordiumNetSdk.SignKey;
using ConcordiumNetSdk.Transactions;
using ConcordiumNetSdk.Types;
using Microsoft.Extensions.Options;
using NSec.Cryptography;

namespace EnergyOrigin.VerifiableEventStore.Services.BlockchainConnector;

public class ConcordiumConnector : IBlockchainConnector, IDisposable
{
    private IOptions<ConcordiumOptions> options;
    private Lazy<ConcordiumNodeClient> concordiumNodeClient;
    private Lazy<TransactionSigner> transactionSigner;

    public ConcordiumConnector(IOptions<ConcordiumOptions> options)
    {
        this.options = options;

        this.concordiumNodeClient = new Lazy<ConcordiumNodeClient>(
            () => new ConcordiumNodeClient(new Connection
            {
                Address = options.Value.Address,
                AuthenticationToken = options.Value.AuthenticationToken
            })
            , true);

        this.transactionSigner = new Lazy<TransactionSigner>(() => CreateSigner(options.Value.SignerFilepath), true);
    }

    private static TransactionSigner CreateSigner(string signerFilepath)
    {
        var keyfileBytes = System.IO.File.ReadAllBytes(signerFilepath);
        Ed25519 algorithm = SignatureAlgorithm.Ed25519;
        var ed25519PrivateKey = Key.Import(algorithm, keyfileBytes, KeyBlobFormat.PkixPrivateKeyText);
        var ed25519TransactionSigner = Ed25519SignKey.From(ed25519PrivateKey.Export(KeyBlobFormat.RawPrivateKey));
        var signer = new TransactionSigner();

        signer.AddSignerEntry(ConcordiumNetSdk.Types.Index.Create(0), ConcordiumNetSdk.Types.Index.Create(0), ed25519TransactionSigner);
        return signer;
    }

    public void Dispose() => concordiumNodeClient.Value.Dispose();

    public async Task<Block?> GetBlock(TransactionReference transactionReference)
    {
        var transactionHash = TransactionHash.From(transactionReference.TransactionHash);

        var transactionStatus = await concordiumNodeClient.Value.GetTransactionStatusAsync(transactionHash);

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
        AccountTransactionService accountTransactionService = new AccountTransactionService(concordiumNodeClient.Value);

        var address = AccountAddress.From(options.Value.AccountAddress);
        var payload = RegisterDataPayload.Create(bytes);
        var transactionHash = await accountTransactionService.SendAccountTransactionAsync(address, payload, transactionSigner.Value);

        return new TransactionReference(transactionHash.AsString);
    }
}
