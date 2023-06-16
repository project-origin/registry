using Grpc.Net.Client;
using ProjectOrigin.Electricity.Example;
using ProjectOrigin.HierarchicalDeterministicKeys.Implementations;
using ProjectOrigin.PedersenCommitment;

public class WithoutWalletFlow
{
    private string registryName;
    private string registryAddress;
    private string area;
    private string signerKeyPath;

    public WithoutWalletFlow(string registryName, string registryAddress, string area, string signerKeyPath)
    {
        this.registryName = registryName;
        this.registryAddress = registryAddress;
        this.area = area;
        this.signerKeyPath = signerKeyPath;
    }

    public async Task<int> Run()
    {
        Console.WriteLine($"Will be using the registry ”{registryName}” on address ”{registryAddress}” with area ”{area}”");

        // We will bee using the Secp256k1 algorithm, same as bitcoin uses.
        var algorithm = new Secp256k1Algorithm();

        // Import the key for the issuing body of the area;
        var encodedKey = File.ReadAllText(signerKeyPath);
        var issuerKey = algorithm.ImportHDPrivateKey(Convert.FromBase64String(encodedKey));

        // Create a new key for the owner since we have no wallet in this example
        var ownerKey = algorithm.GenerateNewPrivateKey();

        // Create channel and client
        var channel = GrpcChannel.ForAddress(registryAddress);
        var client = new ProjectOrigin.Registry.V1.RegistryService.RegistryServiceClient(channel);

        // Instanciate helper with registry and area info
        var helper = new Helper(registryName, area);
        Console.WriteLine($"Preparing to issuing consumption GC");

        // Create a new ConsumptionIssuedEvent, sign it and sent it to the registry.
        var certId = Guid.NewGuid();
        var commitmentInfo = new SecretCommitmentInfo(250);
        var consumptionIssued = helper.CreateConsumptionIssuedEvent(certId, commitmentInfo, ownerKey.PublicKey);

        // Sign the event as a transaction
        var signedTransaction = helper.SignTransaction(consumptionIssued.CertificateId, consumptionIssued, issuerKey);

        // Create and sent the request
        var request = new ProjectOrigin.Registry.V1.SendTransactionsRequest();
        request.Transactions.Add(signedTransaction);
        await client.SendTransactionsAsync(request);
        Console.WriteLine($"Transaction queued");

        // Wait for status of the transaction to be committed.
        var getTransactionStatus = async () => await client.GetTransactionStatusAsync(helper.CreateStatusRequest(signedTransaction));
        await helper.RepeatUntilOrTimeout(getTransactionStatus, (x => x.Status == ProjectOrigin.Registry.V1.TransactionState.Committed), TimeSpan.FromMinutes(1));
        Console.WriteLine($"Transaction Committed");

        return 0;
    }
}
