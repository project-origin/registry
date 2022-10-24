using NSec.Cryptography;
using ProjectOrigin.Electricity.Consumption;
using ProjectOrigin.Electricity.Production.Requests;
using ProjectOrigin.RequestProcessor.Interfaces;
using ProjectOrigin.RequestProcessor.Services;

namespace ProjectOrigin.Electricity.Tests;

public class ProductionAllocatedVerifierTests
{
    private ProductionAllocatedVerifier Verifier(ConsumptionCertificate? pc)
    {
        var mock = new Mock<IModelLoader>();
        mock.Setup(obj => obj.Get<ConsumptionCertificate>(It.IsAny<FederatedStreamId>())).Returns(Task.FromResult((model: pc, eventCount: 1)));
        return new ProductionAllocatedVerifier(new JsonEventSerializer(), mock.Object);
    }

    [Fact]
    public async Task Verifier_AllocateCertificate_Valid()
    {
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var consIssued = Helper.ConsumptionIssued(ownerKey, 250);
        var (cert, sourceParams) = Helper.ProductionIssued(ownerKey, 250);
        var quantity = Helper.Group.Commit(150);

        var request = Helper.CreateProductionAllocation(cert.Id, consIssued.certificate.Id, sourceParams, quantity, ownerKey);
        var verifier = Verifier(consIssued.certificate);

        var result = await verifier.Verify(request, cert);

        Assert.True(result.IsValid, result.ErrorMessage);
    }

    [Fact]
    public async Task Verifier_AllocateCertificate_ProdCertNotFould()
    {
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var consIssued = Helper.ConsumptionIssued(ownerKey, 250);
        var (cert, sourceParams) = Helper.ProductionIssued(ownerKey, 250);
        var quantity = Helper.Group.Commit(150);

        var request = Helper.CreateProductionAllocation(cert.Id, cert.Id, sourceParams, quantity, ownerKey);
        var verifier = Verifier(consIssued.certificate);

        var result = await verifier.Verify(request, null);

        Assert.False(result.IsValid);
        Assert.Equal("Certificate does not exist", result.ErrorMessage);
    }

    [Fact]
    public async Task Verifier_AllocateCertificate_ConsCertNotFould()
    {
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var consIssued = Helper.ConsumptionIssued(ownerKey, 250);
        var (cert, sourceParams) = Helper.ProductionIssued(ownerKey, 250);
        var quantity = Helper.Group.Commit(150);

        var request = Helper.CreateProductionAllocation(cert.Id, cert.Id, sourceParams, quantity, ownerKey);
        var verifier = Verifier(null);

        var result = await verifier.Verify(request, cert);

        Assert.False(result.IsValid);
        Assert.Equal("ConsumptionCertificate does not exist", result.ErrorMessage);
    }
}
