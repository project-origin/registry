using System;
using System.Text;
using System.Threading.Tasks;
using Grpc.Net.Client;
using ProjectOrigin.Electricity.Example;
using ProjectOrigin.Electricity.Example.Extensions;
using ProjectOrigin.HierarchicalDeterministicKeys;
using ProjectOrigin.PedersenCommitment;
using RegistryV1 = ProjectOrigin.Registry.V1;

public class WithoutWalletFlow
{
    public required string Area { get; init; }
    public required string IssuerKey { get; init; }
    public required string ProdRegistryName { get; init; }
    public required string ProdRegistryAddress { get; init; }
    public required string ConsRegistryName { get; init; }
    public required string ConsRegistryAddress { get; init; }

    public async Task<int> Run()
    {
        // Create a new key for the owner since we have no wallet in this example
        var ownerKey = Algorithms.Secp256k1.GenerateNewPrivateKey();

        // Decode text key from base64 string
        var keyText = Encoding.UTF8.GetString(Convert.FromBase64String(IssuerKey));

        // Import the key for the issuing body of the area;
        var issuerKey = Algorithms.Ed25519.ImportPrivateKeyText(keyText);

        // Create channel and client
        var prodChannel = GrpcChannel.ForAddress(ProdRegistryAddress);
        var prodClient = new RegistryV1.RegistryService.RegistryServiceClient(prodChannel);

        // Create channel and client
        var consChannel = GrpcChannel.ForAddress(ConsRegistryAddress);
        var consClient = new RegistryV1.RegistryService.RegistryServiceClient(consChannel);

        // Instanciate eventBuilder with registry and area info
        var eventBuilder = new ProtoEventBuilder
        {
            GridArea = Area,
            Start = new DateTimeOffset(2023, 1, 1, 12, 0, 0, 0, TimeSpan.Zero),
            End = new DateTimeOffset(2023, 1, 1, 13, 0, 0, 0, TimeSpan.Zero)
        };


        // ------------------  Issue Consumption ------------------
        // Create a new ConsumptionIssuedEvent, sign it and sent it to the registry.
        var consCertId = eventBuilder.ToCertId(ConsRegistryName, Guid.NewGuid());
        var consCommitmentInfo = new SecretCommitmentInfo(250);
        {
            Console.WriteLine($"Issuing consumption Granular Certificate");
            var consumptionIssued = eventBuilder.CreateConsumptionIssuedEvent(consCertId, consCommitmentInfo, ownerKey.PublicKey);

            // Sign the event as a transaction
            var signedTransaction = issuerKey.SignTransaction(consumptionIssued.CertificateId, consumptionIssued);

            // Send transaction to registry, and wait for committed state
            await consClient.SendTransactionAndWait(signedTransaction);
        }


        // ------------------  Issue production ------------------
        // Create a new ProductionIssuedEvent, sign it and sent it to the registry.
        var prodCertId = eventBuilder.ToCertId(ProdRegistryName, Guid.NewGuid());
        var prodCommitmentInfo = new SecretCommitmentInfo(350);
        {
            Console.WriteLine($"Issuing Production Granular Certificate");
            var productionIssued = eventBuilder.CreateProductionIssuedEvent(prodCertId, prodCommitmentInfo, ownerKey.PublicKey);

            // Sign the event as a transaction
            var signedTransaction = issuerKey.SignTransaction(productionIssued.CertificateId, productionIssued);

            // Send transaction to registry, and wait for committed state
            await prodClient.SendTransactionAndWait(signedTransaction);
        }


        // ------------------  slice production ------------------
        // define slice commitments
        var prodCommitment250 = new SecretCommitmentInfo(250);
        var prodCommitment100 = new SecretCommitmentInfo(100);
        {
            Console.WriteLine($"Slicing Production Granular Certificate");
            //create slice event, that enables the production certificate to be sliced
            var productionSliceEvent = eventBuilder.CreateSliceEvent(prodCertId, ownerKey.PublicKey, prodCommitmentInfo, prodCommitment250, prodCommitment100);

            // Sign the event as a transaction
            var signedTransaction = ownerKey.SignTransaction(productionSliceEvent.CertificateId, productionSliceEvent);

            // Send transaction to registry, and wait for committed state
            await prodClient.SendTransactionAndWait(signedTransaction);
        }


        // ------------------  allocate production ------------------
        var allocationId = Guid.NewGuid();
        {
            Console.WriteLine($"Allocating Production Granular Certificate");

            // Create allocation event, this can be used for both Certificates
            var allocatedEvent = eventBuilder.CreateAllocatedEvent(allocationId, prodCertId, consCertId, prodCommitment250, consCommitmentInfo);

            // Sign the event as a transaction to the production certificate
            var prodAllocationSignedTransaction = ownerKey.SignTransaction(allocatedEvent.ProductionCertificateId, allocatedEvent);

            // Send transaction to registry, and wait for committed state
            await prodClient.SendTransactionAndWait(prodAllocationSignedTransaction);

            // ------------------ allocate consumption ------------------
            Console.WriteLine($"Allocating Consumption Granular Certificate");

            // Sign the event as a transaction to the consumption certificate
            var consAllocationSignedTransaction = ownerKey.SignTransaction(allocatedEvent.ConsumptionCertificateId, allocatedEvent);

            // Send transaction to registry, and wait for committed state
            await consClient.SendTransactionAndWait(consAllocationSignedTransaction);
        }


        // ------------------  claim production ------------------
        {
            Console.WriteLine($"Claiming Production Granular Certificate");
            var productionClaimEvent = eventBuilder.CreateClaimEvent(allocationId, prodCertId);

            // Sign the event as a transaction
            var signedTransaction = ownerKey.SignTransaction(productionClaimEvent.CertificateId, productionClaimEvent);

            // Send transaction to registry, and wait for committed state
            await prodClient.SendTransactionAndWait(signedTransaction);
        }

        // ------------------  claim consumption ------------------
        {
            Console.WriteLine($"Claiming Consumption Granular Certificate");
            var consumptionClaimEvent = eventBuilder.CreateClaimEvent(allocationId, consCertId);

            // Sign the event as a transaction
            var signedTransaction = ownerKey.SignTransaction(consumptionClaimEvent.CertificateId, consumptionClaimEvent);

            // Send transaction to registry, and wait for committed state
            await consClient.SendTransactionAndWait(signedTransaction);
        }

        return 0;
    }
}
