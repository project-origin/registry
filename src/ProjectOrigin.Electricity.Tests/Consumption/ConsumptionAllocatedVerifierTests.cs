using NSec.Cryptography;
using ProjectOrigin.Electricity.Consumption.Requests;
using ProjectOrigin.Electricity.Production;
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

        var (prodCert, prodParams) = FakeRegister.ProductionIssued(ownerKey, 250);
        var (consCert, consParams) = FakeRegister.ConsumptionIssued(ownerKey, 300);
        var quantity = FakeRegister.Group.Commit(150);
        var (allocationId, _) = prodCert.Allocated(prodParams, consCert.Id, quantity);
        var verifier = Verifier(prodCert);

        var request = FakeRegister.CreateConsumptionAllocationRequest(allocationId, consCert.Id, prodCert.Id, consParams, quantity, ownerKey);

        var result = await verifier.Verify(request, consCert);

        Assert.True(result.IsValid, result.ErrorMessage);
    }

    [Fact]
    public async Task Verifier_AllocateCertificate_CertNotFould()
    {
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);

        var (prodCert, prodParams) = FakeRegister.ProductionIssued(ownerKey, 250);
        var (consCert, consParams) = FakeRegister.ConsumptionIssued(ownerKey, 300);
        var quantity = FakeRegister.Group.Commit(150);
        var (allocationId, _) = prodCert.Allocated(prodParams, consCert.Id, quantity);
        var verifier = Verifier(prodCert);

        var request = FakeRegister.CreateConsumptionAllocationRequest(allocationId, consCert.Id, prodCert.Id, consParams, quantity, ownerKey);

        var result = await verifier.Verify(request, null);

        Assert.False(result.IsValid);
        Assert.Equal("Certificate does not exist", result.ErrorMessage);
    }

    [Fact]
    public async Task Verifier_AllocateCertificate_AllocationNotFound()
    {
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);

        var (prodCert, prodParams) = FakeRegister.ProductionIssued(ownerKey, 250);
        var (consCert, consParams) = FakeRegister.ConsumptionIssued(ownerKey, 300);
        var quantity = FakeRegister.Group.Commit(150);
        var (allocationId, _) = prodCert.Allocated(prodParams, consCert.Id, quantity);
        var verifier = Verifier(prodCert);

        var request = FakeRegister.CreateConsumptionAllocationRequest(Guid.NewGuid(), consCert.Id, prodCert.Id, consParams, quantity, ownerKey);

        var result = await verifier.Verify(request, consCert);

        Assert.False(result.IsValid);
        Assert.Equal("Production not allocated", result.ErrorMessage);
    }

    [Fact]
    public async Task Verifier_AllocateCertificate_DifferentCommitmentsSameQuantity()
    {
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);

        var (prodCert, prodParams) = FakeRegister.ProductionIssued(ownerKey, 250);
        var (consCert, consParams) = FakeRegister.ConsumptionIssued(ownerKey, 300);
        var quantity1 = FakeRegister.Group.Commit(150);
        var quantity2 = FakeRegister.Group.Commit(150);
        var (allocationId, _) = prodCert.Allocated(prodParams, consCert.Id, quantity1);
        var verifier = Verifier(prodCert);

        var request = FakeRegister.CreateConsumptionAllocationRequest(allocationId, consCert.Id, prodCert.Id, consParams, quantity2, ownerKey);

        var result = await verifier.Verify(request, consCert);

        Assert.False(result.IsValid);
        Assert.Equal("Commmitment are not the same", result.ErrorMessage);
    }
}
