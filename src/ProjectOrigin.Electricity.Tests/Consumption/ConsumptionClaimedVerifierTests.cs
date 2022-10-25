using NSec.Cryptography;
using ProjectOrigin.Electricity.Consumption.Requests;
using ProjectOrigin.Electricity.Production;
using ProjectOrigin.RequestProcessor.Interfaces;
using ProjectOrigin.RequestProcessor.Services;

namespace ProjectOrigin.Electricity.Tests;

public class ConsumptionClaimedVerifierTests
{
    private ConsumptionClaimedVerifier Verifier(ProductionCertificate? pc)
    {
        var mock = new Mock<IModelLoader>();
        mock.Setup(obj => obj.Get<ProductionCertificate>(It.IsAny<FederatedStreamId>())).Returns(Task.FromResult((model: pc, eventCount: 1)));
        return new ConsumptionClaimedVerifier(new JsonEventSerializer(), mock.Object);
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
        prodCert.Claimed(allocationId);

        var verifier = Verifier(prodCert);
        var request = FakeRegister.CreateConsumptionClaim(consCert.Id, allocationId, ownerKey);
        var result = await verifier.Verify(request, consCert);

        Assert.True(result.IsValid, result.ErrorMessage);
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
        prodCert.Claimed(allocationId);

        var verifier = Verifier(prodCert);
        var request = FakeRegister.CreateConsumptionClaim(consCert.Id, allocationId, otherKey);
        var result = await verifier.Verify(request, consCert);

        Assert.False(result.IsValid, result.ErrorMessage);
        Assert.Equal("Invalid signature", result.ErrorMessage);
    }

    [Fact]
    public async Task Verifier_AllocateCertificate_NotConsumptionAllocated()
    {
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var quantity = FakeRegister.Group.Commit(150);
        var (prodCert, prodParams) = FakeRegister.ProductionIssued(ownerKey, 250);
        var (consCert, consParams) = FakeRegister.ConsumptionIssued(ownerKey, 300);
        var (allocationId, _) = prodCert.Allocated(prodParams, consCert.Id, quantity);
        prodCert.Claimed(allocationId);

        var verifier = Verifier(prodCert);
        var request = FakeRegister.CreateConsumptionClaim(consCert.Id, allocationId, ownerKey);
        var result = await verifier.Verify(request, consCert);

        Assert.False(result.IsValid, result.ErrorMessage);
        Assert.Equal("Allocation does not exist", result.ErrorMessage);
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
        prodCert.Claimed(allocationId);

        var verifier = Verifier(prodCert);
        var request = FakeRegister.CreateConsumptionClaim(consCert.Id, allocationId, ownerKey);
        var result = await verifier.Verify(request, null);

        Assert.False(result.IsValid, result.ErrorMessage);
        Assert.Equal("Certificate does not exist", result.ErrorMessage);
    }

    [Fact]
    public async Task Verifier_AllocateCertificate_ProductionNotClaimed()
    {
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var quantity = FakeRegister.Group.Commit(150);
        var (prodCert, prodParams) = FakeRegister.ProductionIssued(ownerKey, 250);
        var (consCert, consParams) = FakeRegister.ConsumptionIssued(ownerKey, 300);
        var (allocationId, _) = prodCert.Allocated(prodParams, consCert.Id, quantity);
        consCert.Allocated(allocationId, consParams, prodCert.Id, quantity);

        var verifier = Verifier(prodCert);
        var request = FakeRegister.CreateConsumptionClaim(consCert.Id, allocationId, ownerKey);
        var result = await verifier.Verify(request, consCert);

        Assert.False(result.IsValid, result.ErrorMessage);
        Assert.Equal("Production not claimed", result.ErrorMessage);
    }
}
