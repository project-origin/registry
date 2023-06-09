using System;
using System.Threading.Tasks;
using Moq;
using ProjectOrigin.Electricity.Consumption;
using ProjectOrigin.Electricity.Interfaces;
using ProjectOrigin.Electricity.Production.Verifiers;
using ProjectOrigin.Registry.Utils.Interfaces;
using ProjectOrigin.WalletSystem.Server.HDWallet;
using Xunit;

namespace ProjectOrigin.Electricity.Tests;

public class ProductionClaimedVerifierTests : AssertExtensions
{
    private IKeyAlgorithm _algorithm;
    private ProductionClaimedVerifier _verifier;
    private ConsumptionCertificate? _otherCertificate;

    public ProductionClaimedVerifierTests()
    {
        _algorithm = new Secp256k1Algorithm();

        var modelLoaderMock = new Mock<IRemoteModelLoader>();
        modelLoaderMock.Setup(obj => obj.GetModel<ConsumptionCertificate>(It.IsAny<Common.V1.FederatedStreamId>()))
            .Returns(() => Task.FromResult(_otherCertificate));

        _verifier = new ProductionClaimedVerifier(modelLoaderMock.Object);
    }

    [Fact]
    public async Task ProductionClaimedVerifier_Valid()
    {
        var ownerKey = _algorithm.Create();
        var (consCert, consParams) = FakeRegister.ConsumptionIssued(ownerKey.PublicKey, 250);
        var (prodCert, prodParams) = FakeRegister.ProductionIssued(ownerKey.PublicKey, 250);
        var allocationId = prodCert.Allocated(consCert, prodParams, consParams);
        consCert.Allocated(allocationId, prodCert, prodParams, consParams);
        _otherCertificate = consCert;

        var @event = FakeRegister.CreateClaimedEvent(allocationId, prodCert.Id);
        var transaction = FakeRegister.SignTransaction(@event.CertificateId, @event, ownerKey);

        var result = await _verifier.Verify(transaction, prodCert, @event);

        AssertValid(result);
    }

    [Fact]
    public async Task ProductionClaimedVerifier_Invalid_CertificateNotExists()
    {
        var ownerKey = _algorithm.Create();
        var (consCert, consParams) = FakeRegister.ConsumptionIssued(ownerKey.PublicKey, 250);
        var (prodCert, prodParams) = FakeRegister.ProductionIssued(ownerKey.PublicKey, 250);
        var allocationId = prodCert.Allocated(consCert, prodParams, consParams);
        consCert.Allocated(allocationId, prodCert, prodParams, consParams);
        _otherCertificate = consCert;

        var @event = FakeRegister.CreateClaimedEvent(allocationId, prodCert.Id);
        var transaction = FakeRegister.SignTransaction(@event.CertificateId, @event, ownerKey);

        var result = await _verifier.Verify(transaction, null, @event);

        AssertInvalid(result, "Certificate does not exist");
    }

    [Fact]
    public async Task ProductionClaimedVerifier_Invalid_AllocationNotExist()
    {
        var ownerKey = _algorithm.Create();
        var (consCert, consParams) = FakeRegister.ConsumptionIssued(ownerKey.PublicKey, 250);
        var (prodCert, prodParams) = FakeRegister.ProductionIssued(ownerKey.PublicKey, 250);
        var allocationId = Guid.NewGuid();
        consCert.Allocated(allocationId, prodCert, prodParams, consParams);
        _otherCertificate = consCert;

        var @event = FakeRegister.CreateClaimedEvent(allocationId, prodCert.Id);
        var transaction = FakeRegister.SignTransaction(@event.CertificateId, @event, ownerKey);

        var result = await _verifier.Verify(transaction, prodCert, @event);

        AssertInvalid(result, "Allocation does not exist");
    }

    [Fact]
    public async Task ProductionClaimedVerifier_Invalid_InvalidSignature()
    {
        var ownerKey = _algorithm.Create();
        var otherKey = _algorithm.Create();
        var (consCert, consParams) = FakeRegister.ConsumptionIssued(ownerKey.PublicKey, 250);
        var (prodCert, prodParams) = FakeRegister.ProductionIssued(ownerKey.PublicKey, 250);
        var allocationId = prodCert.Allocated(consCert, prodParams, consParams);
        consCert.Allocated(allocationId, prodCert, prodParams, consParams);
        _otherCertificate = consCert;

        var @event = FakeRegister.CreateClaimedEvent(allocationId, prodCert.Id);
        var transaction = FakeRegister.SignTransaction(@event.CertificateId, @event, otherKey);

        var result = await _verifier.Verify(transaction, prodCert, @event);

        AssertInvalid(result, "Invalid signature for slice");
    }

    [Fact]
    public async Task ProductionClaimedVerifier_Invalid_ConsumptionNotFound()
    {
        var ownerKey = _algorithm.Create();
        var (consCert, consParams) = FakeRegister.ConsumptionIssued(ownerKey.PublicKey, 250);
        var (prodCert, prodParams) = FakeRegister.ProductionIssued(ownerKey.PublicKey, 250);
        var allocationId = prodCert.Allocated(consCert, prodParams, consParams);
        consCert.Allocated(allocationId, prodCert, prodParams, consParams);
        _otherCertificate = null;

        var @event = FakeRegister.CreateClaimedEvent(allocationId, prodCert.Id);
        var transaction = FakeRegister.SignTransaction(@event.CertificateId, @event, ownerKey);

        var result = await _verifier.Verify(transaction, prodCert, @event);

        AssertInvalid(result, "ConsumptionCertificate does not exist");
    }

    [Fact]
    public async Task ProductionClaimedVerifier_Invalid_ConsumptionNotAllocated()
    {
        var ownerKey = _algorithm.Create();
        var (consCert, consParams) = FakeRegister.ConsumptionIssued(ownerKey.PublicKey, 250);
        var (prodCert, prodParams) = FakeRegister.ProductionIssued(ownerKey.PublicKey, 250);
        var allocationId = prodCert.Allocated(consCert, prodParams, consParams);
        _otherCertificate = consCert;

        var @event = FakeRegister.CreateClaimedEvent(allocationId, prodCert.Id);
        var transaction = FakeRegister.SignTransaction(@event.CertificateId, @event, ownerKey);

        var result = await _verifier.Verify(transaction, prodCert, @event);

        AssertInvalid(result, "Consumption not allocated");
    }
}
