using Microsoft.Extensions.Options;
using NSec.Cryptography;
using ProjectOrigin.Electricity.Consumption.Requests;
using ProjectOrigin.Electricity.Production;
using ProjectOrigin.PedersenCommitment;
using ProjectOrigin.RequestProcessor.Interfaces;
using ProjectOrigin.RequestProcessor.Services;

namespace ProjectOrigin.Electricity.Tests;

public class ConsumptionAllocatedVerifierTests
{
    private ConsumptionAllocatedVerifier Verifier(ProductionCertificate? pc)
    {
        var mock = new Mock<IModelLoader>();
        mock.Setup(obj => obj.Get<ProductionCertificate>(It.IsAny<FederatedStreamId>())).Returns(Task.FromResult((model: pc, eventCount: 1)));
        return new ConsumptionAllocatedVerifier(new JsonEventSerializer(), mock.Object);
    }

    [Fact]
    public async Task Verifier_AllocateCertificate_Valid()
    {
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);

        var consIssued = Helper.ConsumptionIssued(ownerKey, 300);
        var prodIssued = Helper.ProductionIssued(ownerKey, 250);
        var quantity = Helper.Group.Commit(150);
        var prodAllocated = Helper.ProductionAllocated(ownerKey, quantity, prodIssued, consIssued.certificate.Id);
        var verifier = Verifier(prodAllocated.certificate);

        var request = Helper.CreateAllocation(prodAllocated.allocationId, consIssued.certificate.Id, prodIssued.certificate.Id, consIssued.parameters, quantity, ownerKey);

        var result = await verifier.Verify(request, consIssued.certificate);

        Assert.True(result.IsValid, result.ErrorMessage);
    }

    [Fact]
    public async Task Verifier_AllocateCertificate_CertNotFould()
    {
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);

        var consIssued = Helper.ConsumptionIssued(ownerKey, 300);
        var prodIssued = Helper.ProductionIssued(ownerKey, 250);
        var quantity = Helper.Group.Commit(150);
        var prodAllocated = Helper.ProductionAllocated(ownerKey, quantity, prodIssued, consIssued.certificate.Id);
        var verifier = Verifier(prodAllocated.certificate);

        var request = Helper.CreateAllocation(prodAllocated.allocationId, consIssued.certificate.Id, prodIssued.certificate.Id, consIssued.parameters, quantity, ownerKey);

        var result = await verifier.Verify(request, null);

        Assert.False(result.IsValid);
        Assert.Equal("Certificate does not exist", result.ErrorMessage);
    }

    [Fact]
    public async Task Verifier_AllocateCertificate_AllocationNotFound()
    {
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);

        var consIssued = Helper.ConsumptionIssued(ownerKey, 300);
        var prodIssued = Helper.ProductionIssued(ownerKey, 250);
        var quantity = Helper.Group.Commit(150);
        var prodAllocated = Helper.ProductionAllocated(ownerKey, quantity, prodIssued, consIssued.certificate.Id);
        var verifier = Verifier(prodAllocated.certificate);

        var request = Helper.CreateAllocation(Guid.NewGuid(), consIssued.certificate.Id, prodIssued.certificate.Id, consIssued.parameters, quantity, ownerKey);

        var result = await verifier.Verify(request, consIssued.certificate);

        Assert.False(result.IsValid);
        Assert.Equal("Production not allocated", result.ErrorMessage);
    }
}
