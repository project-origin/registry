using Grpc.Net.Client;
using ProjectOrigin.Electricity.Example;
using ProjectOrigin.HierarchicalDeterministicKeys.Implementations;
using ProjectOrigin.PedersenCommitment;
using ProjectOrigin.Registry.V1;

public class WithoutWalletFlow
{
    private string area;
    private string signerKeyPath;
    private string prodRegistryName;
    private string prodRegistryAddress;
    private string consRegistryName;
    private string consRegistryAddress;

    public WithoutWalletFlow(
        string area,
        string signerKeyPath,
        string prodRegistryName,
        string prodRegistryAddress,
        string consRegistryName,
        string consRegistryAddress)
    {
        this.area = area;
        this.signerKeyPath = signerKeyPath;
        this.prodRegistryName = prodRegistryName;
        this.prodRegistryAddress = prodRegistryAddress;
        this.consRegistryName = consRegistryName;
        this.consRegistryAddress = consRegistryAddress;
    }

    public async Task<int> Run()
    {
        // We will bee using the Secp256k1 algorithm, same as bitcoin uses.
        var algorithm = new Secp256k1Algorithm();

        // Import the key for the issuing body of the area;
        var encodedKey = File.ReadAllText(signerKeyPath);
        var issuerKey = algorithm.ImportHDPrivateKey(Convert.FromBase64String(encodedKey));

        // Create a new key for the owner since we have no wallet in this example
        var ownerKey = algorithm.GenerateNewPrivateKey();

        // Create channel and client
        var prodChannel = GrpcChannel.ForAddress(prodRegistryAddress);
        var prodClient = new ProjectOrigin.Registry.V1.RegistryService.RegistryServiceClient(prodChannel);

        // Create channel and client
        var consChannel = GrpcChannel.ForAddress(consRegistryAddress);
        var consClient = new ProjectOrigin.Registry.V1.RegistryService.RegistryServiceClient(consChannel);

        // Instanciate helper with registry and area info
        var helper = new Helper(area);


        // ------------------  Issue Consumption ------------------
        // Create a new ConsumptionIssuedEvent, sign it and sent it to the registry.
        var consCertId = helper.ToCertId(consRegistryName, Guid.NewGuid());
        var consCommitmentInfo = new SecretCommitmentInfo(250);
        {
            Console.WriteLine($"Issuing consumption GC");
            var consumptionIssued = helper.CreateConsumptionIssuedEvent(consCertId, consCommitmentInfo, ownerKey.PublicKey);

            // Sign the event as a transaction
            var signedTransaction = helper.SignTransaction(consumptionIssued.CertificateId, consumptionIssued, issuerKey);

            // Send transaction to registry, and wait for committed state
            await SendTransactionAndWait(consClient, helper, signedTransaction);
        }


        // ------------------  Issue production ------------------
        // Create a new ProductionIssuedEvent, sign it and sent it to the registry.
        var prodCertId = helper.ToCertId(prodRegistryName, Guid.NewGuid());
        var prodCommitmentInfo = new SecretCommitmentInfo(350);
        {
            Console.WriteLine($"Issuing Production Granular Certificate");
            var productionIssued = helper.CreateProductionIssuedEvent(prodCertId, prodCommitmentInfo, ownerKey.PublicKey);

            // Sign the event as a transaction
            var signedTransaction = helper.SignTransaction(productionIssued.CertificateId, productionIssued, issuerKey);

            // Send transaction to registry, and wait for committed state
            await SendTransactionAndWait(prodClient, helper, signedTransaction);
        }


        // ------------------  slice production ------------------
        // define slice commitments
        var prodCommitment250 = new SecretCommitmentInfo(250);
        var prodCommitment100 = new SecretCommitmentInfo(100);
        {
            Console.WriteLine($"Slicing Production Granular Certificate");
            //create slice event, that enables the production certificate to be sliced
            var productionSliceEvent = helper.CreateSliceEvent(prodCertId, ownerKey.PublicKey, prodCommitmentInfo, prodCommitment250, prodCommitment100);

            // Sign the event as a transaction
            var signedTransaction = helper.SignTransaction(productionSliceEvent.CertificateId, productionSliceEvent, ownerKey);

            // Send transaction to registry, and wait for committed state
            await SendTransactionAndWait(prodClient, helper, signedTransaction);
        }


        // ------------------  allocate production ------------------
        var allocationId = Guid.NewGuid();
        {
            Console.WriteLine($"Allocating Production Granular Certificate");

            // Create allocation event, this can be used for both Certificates
            var allocatedEvent = helper.CreateAllocatedEvent(allocationId, prodCertId, consCertId, prodCommitment250, consCommitmentInfo);

            // Sign the event as a transaction to the production certificate
            var prodAllocationSignedTransaction = helper.SignTransaction(allocatedEvent.ProductionCertificateId, allocatedEvent, ownerKey);

            // Send transaction to registry, and wait for committed state
            await SendTransactionAndWait(prodClient, helper, prodAllocationSignedTransaction);

            // ------------------ allocate consumption ------------------
            Console.WriteLine($"Allocating Consumption Granular Certificate");

            // Sign the event as a transaction to the consumption certificate
            var consAllocationSignedTransaction = helper.SignTransaction(allocatedEvent.ConsumptionCertificateId, allocatedEvent, ownerKey);

            // Send transaction to registry, and wait for committed state
            await SendTransactionAndWait(consClient, helper, consAllocationSignedTransaction);
        }


        // ------------------  claim production ------------------
        {
            Console.WriteLine($"Claiming Production Granular Certificate");
            var productionClaimEvent = helper.CreateClaimEvent(allocationId, prodCertId);

            // Sign the event as a transaction
            var signedTransaction = helper.SignTransaction(productionClaimEvent.CertificateId, productionClaimEvent, ownerKey);

            // Send transaction to registry, and wait for committed state
            await SendTransactionAndWait(prodClient, helper, signedTransaction);
        }

        // ------------------  claim consumption ------------------
        {
            Console.WriteLine($"Claiming Consumption Granular Certificate");
            var consumptionClaimEvent = helper.CreateClaimEvent(allocationId, consCertId);

            // Sign the event as a transaction
            var signedTransaction = helper.SignTransaction(consumptionClaimEvent.CertificateId, consumptionClaimEvent, ownerKey);

            // Send transaction to registry, and wait for committed state
            await SendTransactionAndWait(consClient, helper, signedTransaction);
        }

        return 0;
    }

    private static async Task SendTransactionAndWait(RegistryService.RegistryServiceClient client, Helper helper, Transaction singedTransaction)
    {
        var consClaimRequest = new ProjectOrigin.Registry.V1.SendTransactionsRequest();
        consClaimRequest.Transactions.Add(singedTransaction);
        await client.SendTransactionsAsync(consClaimRequest);
        Console.WriteLine($"- transaction queued");

        // Wait for status of the transaction to be committed.
        await helper.WaitForCommittedOrTimeout(client, singedTransaction, TimeSpan.FromMinutes(1));
        Console.WriteLine($"- transaction committed");
    }
}
