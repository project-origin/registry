using NSec.Cryptography;
using ProjectOrigin.Electricity.Consumption;
using ProjectOrigin.Electricity.Models;
using ProjectOrigin.Electricity.Production.Requests;
using ProjectOrigin.Register.LineProcessor.Interfaces;
using ProjectOrigin.Register.LineProcessor.Models;

namespace ProjectOrigin.Electricity.Tests;

public class ProductionAllocatedVerifierTests
{
    private ProductionAllocatedVerifier Verifier(ConsumptionCertificate? pc)
    {
        var mock = new Mock<IModelLoader>();
        mock.Setup(obj => obj.Get<ConsumptionCertificate>(It.IsAny<FederatedStreamId>())).Returns(Task.FromResult((model: pc, eventCount: 1)));
        return new ProductionAllocatedVerifier(mock.Object);
    }

    [Fact]
    public async Task Verifier_AllocateCertificate_Valid()
    {
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var consIssued = FakeRegister.ConsumptionIssued(ownerKey.PublicKey, 250);
        var (cert, sourceParams) = FakeRegister.ProductionIssued(ownerKey.PublicKey, 250);
        var quantity = FakeRegister.Group.Commit(150);

        var request = FakeRegister.CreateProductionAllocationRequest(cert.Id, consIssued.certificate.Id, sourceParams, quantity, ownerKey);
        var verifier = Verifier(consIssued.certificate);

        var result = await verifier.Verify(request, cert);

        Assert.IsType<VerificationResult.Valid>(result);
    }

    [Fact]
    public async Task Verifier_AllocateCertificate_InvalidArea()
    {
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var consIssued = FakeRegister.ConsumptionIssued(ownerKey.PublicKey, 250, area: "DK1");
        var (cert, sourceParams) = FakeRegister.ProductionIssued(ownerKey.PublicKey, 250, area: "DK2");
        var quantity = FakeRegister.Group.Commit(150);

        var request = FakeRegister.CreateProductionAllocationRequest(cert.Id, consIssued.certificate.Id, sourceParams, quantity, ownerKey);
        var verifier = Verifier(consIssued.certificate);

        var result = await verifier.Verify(request, cert);

        var invalid = result as VerificationResult.Invalid;
        Assert.NotNull(invalid);
        Assert.Equal("Certificates are not in the same area", invalid!.ErrorMessage);
    }

    [Fact]
    public async Task Verifier_AllocateCertificate_InvalidPeriod()
    {
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var consIssued = FakeRegister.ConsumptionIssued(ownerKey.PublicKey, 250);
        var hourLater = new TimePeriod(consIssued.certificate.Period.DateTimeTo, consIssued.certificate.Period.DateTimeTo.AddHours(1));
        var (cert, sourceParams) = FakeRegister.ProductionIssued(ownerKey.PublicKey, 250, period: hourLater);
        var quantity = FakeRegister.Group.Commit(150);

        var request = FakeRegister.CreateProductionAllocationRequest(cert.Id, consIssued.certificate.Id, sourceParams, quantity, ownerKey);
        var verifier = Verifier(consIssued.certificate);

        var result = await verifier.Verify(request, cert);

        var invalid = result as VerificationResult.Invalid;
        Assert.NotNull(invalid);
        Assert.Equal("Certificates are not in the same period", invalid!.ErrorMessage);
    }

    [Fact]
    public async Task Verifier_AllocateCertificate_ProdCertNotFould()
    {
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var consIssued = FakeRegister.ConsumptionIssued(ownerKey.PublicKey, 250);
        var (cert, sourceParams) = FakeRegister.ProductionIssued(ownerKey.PublicKey, 250);
        var quantity = FakeRegister.Group.Commit(150);

        var request = FakeRegister.CreateProductionAllocationRequest(cert.Id, cert.Id, sourceParams, quantity, ownerKey);
        var verifier = Verifier(consIssued.certificate);

        var result = await verifier.Verify(request, null);

        var invalid = result as VerificationResult.Invalid;
        Assert.NotNull(invalid);
        Assert.Equal("Certificate does not exist", invalid!.ErrorMessage);
    }

    [Fact]
    public async Task Verifier_AllocateCertificate_ConsCertNotFould()
    {
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var consIssued = FakeRegister.ConsumptionIssued(ownerKey.PublicKey, 250);
        var (cert, sourceParams) = FakeRegister.ProductionIssued(ownerKey.PublicKey, 250);
        var quantity = FakeRegister.Group.Commit(150);

        var request = FakeRegister.CreateProductionAllocationRequest(cert.Id, cert.Id, sourceParams, quantity, ownerKey);
        var verifier = Verifier(null);

        var result = await verifier.Verify(request, cert);

        var invalid = result as VerificationResult.Invalid;
        Assert.NotNull(invalid);
        Assert.Equal("ConsumptionCertificate does not exist", invalid!.ErrorMessage);
    }
}
