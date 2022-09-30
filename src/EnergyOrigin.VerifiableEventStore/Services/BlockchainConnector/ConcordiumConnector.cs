using ConcordiumNetSdk;
using ConcordiumNetSdk.SignKey;
using ConcordiumNetSdk.Transactions;
using ConcordiumNetSdk.Types;
using Microsoft.Extensions.Options;
using NSec.Cryptography;

namespace EnergyOrigin.VerifiableEventStore.Services.BlockchainConnector;

public class ConcordiumConnector : IBlockchainConnector, IDisposable
{
    private ConcordiumNodeClient concordiumNodeClient;
    private IOptions<ConcordiumOptions> options;
    private TransactionSigner transactionSigner;

    public ConcordiumConnector(IOptions<ConcordiumOptions> options)
    {
        this.options = options;

        Connection connection = new Connection
        {
            Address = options.Value.Address,
            AuthenticationToken = options.Value.AuthenticationToken
        };
        concordiumNodeClient = new ConcordiumNodeClient(connection);
        var signer = CreateSigner(options.Value.SignerFilepath);
        this.transactionSigner = signer;
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

    public void Dispose() => concordiumNodeClient.Dispose();

    public async Task<Block?> GetBlock(TransactionReference transactionReference)
    {
        var transactionHash = TransactionHash.From(transactionReference.TransactionId);

        var status = await concordiumNodeClient.GetTransactionStatusAsync(transactionHash);

        if (status != null)
        {
            throw new NotImplementedException("I could not find where to get block id.");
            return new Block("", status.Status == TransactionStatusType.Finalized);
        }

        return null;
    }

    public async Task<TransactionReference> PublishBytes(byte[] bytes)
    {
        AccountTransactionService accountTransactionService = new AccountTransactionService(concordiumNodeClient);

        var address = AccountAddress.From(options.Value.AccountAddress);
        var payload = RegisterDataPayload.Create(bytes);
        var transactionHash = await accountTransactionService.SendAccountTransactionAsync(address, payload, transactionSigner);

        return new TransactionReference(transactionHash.AsString);
    }
}
