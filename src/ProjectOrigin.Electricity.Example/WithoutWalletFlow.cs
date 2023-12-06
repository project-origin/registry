using System;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Grpc.Net.Client;
using ProjectOrigin.Common.V1;
using ProjectOrigin.Electricity.Example.Extensions;
using ProjectOrigin.Electricity.V1;
using ProjectOrigin.HierarchicalDeterministicKeys;
using ProjectOrigin.HierarchicalDeterministicKeys.Interfaces;
using ProjectOrigin.PedersenCommitment;
using ProjectOrigin.Registry.V1;
using RegistryV1 = ProjectOrigin.Registry.V1;

namespace ProjectOrigin.Electricity.Example;

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
        var consCertId = ProtoEventBuilder.ToCertId(ConsRegistryName, Guid.NewGuid());
        var consCommitmentInfo = new SecretCommitmentInfo(250);
        Console.WriteLine($"Issuing consumption Granular Certificate");
        await IssueConsumptionCertificate(ownerKey, issuerKey, consClient, eventBuilder, consCertId, consCommitmentInfo);


        // ------------------  Issue production ------------------
        // Create a new ProductionIssuedEvent, sign it and sent it to the registry.
        var prodCertId = ProtoEventBuilder.ToCertId(ProdRegistryName, Guid.NewGuid());
        var prodCommitmentInfo = new SecretCommitmentInfo(350);
        Console.WriteLine($"Issuing Production Granular Certificate");
        await IssueProductionCertificate(ownerKey, issuerKey, prodClient, eventBuilder, prodCertId, prodCommitmentInfo);


        // ------------------  slice production ------------------
        // define slice commitments
        var prodCommitment250 = new SecretCommitmentInfo(250);
        var prodCommitment100 = new SecretCommitmentInfo(100);
        Console.WriteLine($"Slicing Production Granular Certificate");
        await SliceCertificate(ownerKey, prodClient, prodCertId, prodCommitmentInfo, prodCommitment250, prodCommitment100);


        // ------------------  allocate ------------------
        Console.WriteLine($"Allocating Production Granular Certificate");
        var allocationId = Guid.NewGuid();
        var allocatedEvent = ProtoEventBuilder.CreateAllocatedEvent(allocationId, prodCertId, consCertId, prodCommitment250, consCommitmentInfo);


        // ------------------ allocate production ------------------
        Console.WriteLine($"Allocating Production Granular Certificate");
        await SendAllocation(ownerKey, prodClient, allocatedEvent, allocatedEvent.ProductionCertificateId);


        // ------------------ allocate consumption ------------------
        Console.WriteLine($"Allocating Consumption Granular Certificate");
        await SendAllocation(ownerKey, consClient, allocatedEvent, allocatedEvent.ConsumptionCertificateId);


        // ------------------  claim production ------------------
        Console.WriteLine($"Claiming Production Granular Certificate");
        await NewMethod(ownerKey, prodClient, prodCertId, allocationId);


        // ------------------  claim consumption ------------------
        Console.WriteLine($"Claiming Consumption Granular Certificate");
        await NewMethod(ownerKey, consClient, consCertId, allocationId);

        return 0;
    }

    private static async Task NewMethod(IHDPrivateKey ownerKey, RegistryService.RegistryServiceClient prodClient, FederatedStreamId prodCertId, Guid allocationId)
    {
        var productionClaimEvent = ProtoEventBuilder.CreateClaimEvent(allocationId, prodCertId);

        // Sign the event as a transaction
        var signedTransaction = ownerKey.SignTransaction(productionClaimEvent.CertificateId, productionClaimEvent);

        // Send transaction to registry, and wait for committed state
        await prodClient.SendTransactionAndWait(signedTransaction);
    }

    private static async Task SendAllocation(IHDPrivateKey ownerKey, RegistryService.RegistryServiceClient prodClient, AllocatedEvent allocatedEvent, FederatedStreamId certificateId)
    {
        // Sign the event as a transaction to the production certificate
        var prodAllocationSignedTransaction = ownerKey.SignTransaction(certificateId, allocatedEvent);

        // Send transaction to registry, and wait for committed state
        await prodClient.SendTransactionAndWait(prodAllocationSignedTransaction);
    }

    private static async Task IssueConsumptionCertificate(IHDPrivateKey ownerKey, IPrivateKey issuerKey, RegistryService.RegistryServiceClient consClient, ProtoEventBuilder eventBuilder, FederatedStreamId consCertId, SecretCommitmentInfo consCommitmentInfo)
    {
        var consumptionIssued = eventBuilder.CreateConsumptionIssuedEvent(consCertId, consCommitmentInfo, ownerKey.PublicKey);

        // Sign the event as a transaction
        var signedTransaction = issuerKey.SignTransaction(consumptionIssued.CertificateId, consumptionIssued);

        // Send transaction to registry, and wait for committed state
        await consClient.SendTransactionAndWait(signedTransaction);
    }

    private static async Task IssueProductionCertificate(IHDPrivateKey ownerKey, IPrivateKey issuerKey, RegistryService.RegistryServiceClient prodClient, ProtoEventBuilder eventBuilder, FederatedStreamId prodCertId, SecretCommitmentInfo prodCommitmentInfo)
    {
        var productionIssued = eventBuilder.CreateProductionIssuedEvent(prodCertId, prodCommitmentInfo, ownerKey.PublicKey);

        // Sign the event as a transaction
        var signedTransaction = issuerKey.SignTransaction(productionIssued.CertificateId, productionIssued);

        // Send transaction to registry, and wait for committed state
        await prodClient.SendTransactionAndWait(signedTransaction);
    }

    private static async Task SliceCertificate(IHDPrivateKey ownerKey, RegistryService.RegistryServiceClient prodClient, FederatedStreamId prodCertId, SecretCommitmentInfo prodCommitmentInfo, SecretCommitmentInfo prodCommitment250, SecretCommitmentInfo prodCommitment100)
    {
        var productionSliceEvent = ProtoEventBuilder.CreateSliceEvent(prodCertId, ownerKey.PublicKey, prodCommitmentInfo, prodCommitment250, prodCommitment100);

        // Sign the event as a transaction
        var signedTransaction = ownerKey.SignTransaction(productionSliceEvent.CertificateId, productionSliceEvent);

        // Send transaction to registry, and wait for committed state
        await prodClient.SendTransactionAndWait(signedTransaction);
    }
}
