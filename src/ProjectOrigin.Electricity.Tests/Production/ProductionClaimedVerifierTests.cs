using NSec.Cryptography;
using ProjectOrigin.Electricity.Consumption;
using ProjectOrigin.Electricity.Production.Requests;
using ProjectOrigin.RequestProcessor.Interfaces;
using ProjectOrigin.RequestProcessor.Services;

namespace ProjectOrigin.Electricity.Tests;

public class ProductionClaimedVerifierTests
{
    private ProductionClaimedVerifier Verifier(ConsumptionCertificate? pc)
    {
        var mock = new Mock<IModelLoader>();
        mock.Setup(obj => obj.Get<ConsumptionCertificate>(It.IsAny<FederatedStreamId>())).Returns(Task.FromResult((model: pc, eventCount: 1)));
        return new ProductionClaimedVerifier(new JsonEventSerializer(), mock.Object);
    }

    [Fact]
    public async Task Verifier_AllocateCertificate_Valid()
    {
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var quantity = FakeRegister.Group.Commit(150);
        var (prodCert, prodParams) = FakeRegister.ProductionIssued(ownerKey, 250);
        var (consCert, consParams) = FakeRegister.ConsumptionIssued(ownerKey, 300);
        var (allocationId, _) = prodCert.Allocated(prodParams, consCert.Id, quantity);
        consCert.Allocated(allocationId, consParams, prodCert.Id, quantity);

        var verifier = Verifier(consCert);
        var request = FakeRegister.CreateProductionClaim(prodCert.Id, allocationId, ownerKey);
        var result = await verifier.Verify(request, prodCert);

        Assert.True(result.IsValid, result.ErrorMessage);
    }

    [Fact]
    public async Task Verifier_AllocateCertificate_CertNotFound()
    {
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var quantity = FakeRegister.Group.Commit(150);
        var (prodCert, prodParams) = FakeRegister.ProductionIssued(ownerKey, 250);
        var (consCert, consParams) = FakeRegister.ConsumptionIssued(ownerKey, 300);
        var (allocationId, _) = prodCert.Allocated(prodParams, consCert.Id, quantity);
        consCert.Allocated(allocationId, consParams, prodCert.Id, quantity);

        var verifier = Verifier(consCert);
        var request = FakeRegister.CreateProductionClaim(consCert.Id, allocationId, ownerKey);
        var result = await verifier.Verify(request, null);

        Assert.False(result.IsValid, result.ErrorMessage);
        Assert.Equal("Certificate does not exist", result.ErrorMessage);
    }

    [Fact]
    public async Task Verifier_AllocateCertificate_ConsumptionNotAllocated()
    {
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var quantity = FakeRegister.Group.Commit(150);
        var (prodCert, prodParams) = FakeRegister.ProductionIssued(ownerKey, 250);
        var (consCert, consParams) = FakeRegister.ConsumptionIssued(ownerKey, 300);
        var (allocationId, _) = prodCert.Allocated(prodParams, consCert.Id, quantity);

        var verifier = Verifier(consCert);
        var request = FakeRegister.CreateProductionClaim(prodCert.Id, allocationId, ownerKey);
        var result = await verifier.Verify(request, prodCert);

        Assert.False(result.IsValid, result.ErrorMessage);
        Assert.Equal("Consumption not allocated", result.ErrorMessage);
    }

    [Fact]
    public async Task Verifier_AllocateCertificate_ProdNotAllocated()
    {
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var quantity = FakeRegister.Group.Commit(150);
        var (prodCert, prodParams) = FakeRegister.ProductionIssued(ownerKey, 250);
        var (consCert, consParams) = FakeRegister.ConsumptionIssued(ownerKey, 300);
        var allocationId = Guid.NewGuid();
        consCert.Allocated(allocationId, consParams, prodCert.Id, quantity);

        var verifier = Verifier(consCert);
        var request = FakeRegister.CreateProductionClaim(prodCert.Id, allocationId, ownerKey);
        var result = await verifier.Verify(request, prodCert);

        Assert.False(result.IsValid, result.ErrorMessage);
        Assert.Equal("Allocation does not exist", result.ErrorMessage);
    }

    [Fact]
    public async Task Verifier_AllocateCertificate_InvalidSignature()
    {
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var otherKey = Key.Create(SignatureAlgorithm.Ed25519);
        var quantity = FakeRegister.Group.Commit(150);
        var (prodCert, prodParams) = FakeRegister.ProductionIssued(ownerKey, 250);
        var (consCert, consParams) = FakeRegister.ConsumptionIssued(ownerKey, 300);
        var (allocationId, _) = prodCert.Allocated(prodParams, consCert.Id, quantity);
        consCert.Allocated(allocationId, consParams, prodCert.Id, quantity);

        var verifier = Verifier(consCert);
        var request = FakeRegister.CreateProductionClaim(prodCert.Id, allocationId, otherKey);
        var result = await verifier.Verify(request, prodCert);

        Assert.False(result.IsValid, result.ErrorMessage);
        Assert.Equal("Invalid signature", result.ErrorMessage);
    }
}
