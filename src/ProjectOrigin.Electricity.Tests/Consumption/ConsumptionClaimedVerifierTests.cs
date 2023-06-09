using System.Threading.Tasks;
using Moq;
using ProjectOrigin.Electricity.Consumption.Verifiers;
using ProjectOrigin.Electricity.Production;
using ProjectOrigin.HierarchicalDeterministicKeys.Implementations;
using ProjectOrigin.HierarchicalDeterministicKeys.Interfaces;
using ProjectOrigin.Registry.Utils.Interfaces;
using Xunit;

namespace ProjectOrigin.Electricity.Tests;

public class ConsumptionClaimedVerifierTests : AssertExtensions
{
    private IHDAlgorithm _algorithm;
    private ConsumptionClaimedVerifier _verifier;
    private ProductionCertificate? _otherCertificate;

    public ConsumptionClaimedVerifierTests()
    {
        _algorithm = new Secp256k1Algorithm();

        var modelLoaderMock = new Mock<IRemoteModelLoader>();
        modelLoaderMock.Setup(obj => obj.GetModel<ProductionCertificate>(It.IsAny<Common.V1.FederatedStreamId>()))
            .Returns(() => Task.FromResult(_otherCertificate));

        _verifier = new ConsumptionClaimedVerifier(modelLoaderMock.Object);
    }

    [Fact]
    public async Task ProductionClaimedVerifier_Valid()
    {
        var ownerKey = _algorithm.GenerateNewPrivateKey();
        var (consCert, consParams) = FakeRegister.ConsumptionIssued(ownerKey.PublicKey, 250);
        var (prodCert, prodParams) = FakeRegister.ProductionIssued(ownerKey.PublicKey, 250);
        var allocationId = prodCert.Allocated(consCert, prodParams, consParams);
        consCert.Allocated(allocationId, prodCert, prodParams, consParams);
        prodCert.Claimed(allocationId);
        _otherCertificate = prodCert;

        var @event = FakeRegister.CreateClaimedEvent(allocationId, consCert.Id);
        var transaction = FakeRegister.SignTransaction(@event.CertificateId, @event, ownerKey);

        var result = await _verifier.Verify(transaction, consCert, @event);

        AssertValid(result);
    }

    [Fact]
    public async Task ProductionClaimedVerifier_Invalid_CertificateNotExists()
    {
        var ownerKey = _algorithm.GenerateNewPrivateKey();
        var (consCert, consParams) = FakeRegister.ConsumptionIssued(ownerKey.PublicKey, 250);
        var (prodCert, prodParams) = FakeRegister.ProductionIssued(ownerKey.PublicKey, 250);
        var allocationId = prodCert.Allocated(consCert, prodParams, consParams);
        consCert.Allocated(allocationId, prodCert, prodParams, consParams);
        prodCert.Claimed(allocationId);
        _otherCertificate = prodCert;

        var @event = FakeRegister.CreateClaimedEvent(allocationId, consCert.Id);
        var transaction = FakeRegister.SignTransaction(@event.CertificateId, @event, ownerKey);

        var result = await _verifier.Verify(transaction, null, @event);

        AssertInvalid(result, "Certificate does not exist");
    }

    [Fact]
    public async Task ProductionClaimedVerifier_Invalid_AllocationNotExist()
    {
        var ownerKey = _algorithm.GenerateNewPrivateKey();
        var (consCert, consParams) = FakeRegister.ConsumptionIssued(ownerKey.PublicKey, 250);
        var (prodCert, prodParams) = FakeRegister.ProductionIssued(ownerKey.PublicKey, 250);
        var allocationId = prodCert.Allocated(consCert, prodParams, consParams);
        prodCert.Claimed(allocationId);
        _otherCertificate = prodCert;

        var @event = FakeRegister.CreateClaimedEvent(allocationId, consCert.Id);
        var transaction = FakeRegister.SignTransaction(@event.CertificateId, @event, ownerKey);

        var result = await _verifier.Verify(transaction, consCert, @event);

        AssertInvalid(result, "Allocation does not exist");
    }

    [Fact]
    public async Task ProductionClaimedVerifier_Invalid_InvalidSignature()
    {
        var ownerKey = _algorithm.GenerateNewPrivateKey();
        var otherKey = _algorithm.GenerateNewPrivateKey();
        var (consCert, consParams) = FakeRegister.ConsumptionIssued(ownerKey.PublicKey, 250);
        var (prodCert, prodParams) = FakeRegister.ProductionIssued(ownerKey.PublicKey, 250);
        var allocationId = prodCert.Allocated(consCert, prodParams, consParams);
        consCert.Allocated(allocationId, prodCert, prodParams, consParams);
        prodCert.Claimed(allocationId);
        _otherCertificate = prodCert;

        var @event = FakeRegister.CreateClaimedEvent(allocationId, consCert.Id);
        var transaction = FakeRegister.SignTransaction(@event.CertificateId, @event, otherKey);

        var result = await _verifier.Verify(transaction, consCert, @event);

        AssertInvalid(result, "Invalid signature for slice");
    }

    [Fact]
    public async Task ProductionClaimedVerifier_Invalid_ConsumptionNotFound()
    {
        var ownerKey = _algorithm.GenerateNewPrivateKey();
        var (consCert, consParams) = FakeRegister.ConsumptionIssued(ownerKey.PublicKey, 250);
        var (prodCert, prodParams) = FakeRegister.ProductionIssued(ownerKey.PublicKey, 250);
        var allocationId = prodCert.Allocated(consCert, prodParams, consParams);
        consCert.Allocated(allocationId, prodCert, prodParams, consParams);
        prodCert.Claimed(allocationId);
        _otherCertificate = null;

        var @event = FakeRegister.CreateClaimedEvent(allocationId, consCert.Id);
        var transaction = FakeRegister.SignTransaction(@event.CertificateId, @event, ownerKey);

        var result = await _verifier.Verify(transaction, consCert, @event);

        AssertInvalid(result, "ProductionCertificate does not exist");
    }

    [Fact]
    public async Task ProductionClaimedVerifier_Invalid_ConsumptionNotAllocated()
    {
        var ownerKey = _algorithm.GenerateNewPrivateKey();
        var (consCert, consParams) = FakeRegister.ConsumptionIssued(ownerKey.PublicKey, 250);
        var (prodCert, prodParams) = FakeRegister.ProductionIssued(ownerKey.PublicKey, 250);
        var allocationId = prodCert.Allocated(consCert, prodParams, consParams);
        consCert.Allocated(allocationId, prodCert, prodParams, consParams);
        _otherCertificate = prodCert;

        var @event = FakeRegister.CreateClaimedEvent(allocationId, consCert.Id);
        var transaction = FakeRegister.SignTransaction(@event.CertificateId, @event, ownerKey);

        var result = await _verifier.Verify(transaction, consCert, @event);

        AssertInvalid(result, "Production not claimed");
    }
}
