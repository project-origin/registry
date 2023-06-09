using System;
using System.Threading.Tasks;
using Moq;
using ProjectOrigin.Electricity.Consumption.Verifiers;
using ProjectOrigin.Electricity.Interfaces;
using ProjectOrigin.Electricity.Production;
using ProjectOrigin.Registry.Utils.Interfaces;
using ProjectOrigin.WalletSystem.Server.HDWallet;
using Xunit;

namespace ProjectOrigin.Electricity.Tests;

public class ConsumptionAllocatedVerifierTests : AssertExtensions
{
    private IKeyAlgorithm _algorithm;
    private ConsumptionAllocatedVerifier _verifier;
    private ProductionCertificate? _otherCertificate;

    public ConsumptionAllocatedVerifierTests()
    {
        _algorithm = new Secp256k1Algorithm();

        var modelLoaderMock = new Mock<IRemoteModelLoader>();
        modelLoaderMock.Setup(obj => obj.GetModel<ProductionCertificate>(It.IsAny<Common.V1.FederatedStreamId>()))
            .Returns(() => Task.FromResult(_otherCertificate));

        _verifier = new ConsumptionAllocatedVerifier(modelLoaderMock.Object);
    }


    [Fact]
    public async Task Verifier_AllocateCertificate_Valid()
    {
        var ownerKey = _algorithm.Create();
        var (consCert, consParams) = FakeRegister.ConsumptionIssued(ownerKey.PublicKey, 250);
        var (prodCert, prodParams) = FakeRegister.ProductionIssued(ownerKey.PublicKey, 250);
        var allocationId = prodCert.Allocated(consCert, prodParams, consParams);
        _otherCertificate = prodCert;

        var @event = FakeRegister.CreateAllocationEvent(allocationId, prodCert.Id, consCert.Id, prodParams, consParams);
        var transaction = FakeRegister.SignTransaction(@event.ConsumptionCertificateId, @event, ownerKey);

        var result = await _verifier.Verify(transaction, consCert, @event);

        AssertValid(result);
    }

    [Fact]
    public async Task Verifier_CertNotFound_Invalid()
    {
        var ownerKey = _algorithm.Create();
        var (consCert, consParams) = FakeRegister.ConsumptionIssued(ownerKey.PublicKey, 250);
        var (prodCert, prodParams) = FakeRegister.ProductionIssued(ownerKey.PublicKey, 250);
        var allocationId = prodCert.Allocated(consCert, prodParams, consParams);
        _otherCertificate = prodCert;

        var @event = FakeRegister.CreateAllocationEvent(allocationId, prodCert.Id, consCert.Id, prodParams, consParams);
        var transaction = FakeRegister.SignTransaction(@event.ConsumptionCertificateId, @event, ownerKey);

        var result = await _verifier.Verify(transaction, null, @event);

        AssertInvalid(result, "Certificate does not exist");
    }

    [Fact]
    public async Task Verifier_SliceNotFound_Invalid()
    {
        var ownerKey = _algorithm.Create();
        var (consCert, consParams) = FakeRegister.ConsumptionIssued(ownerKey.PublicKey, 250);
        var (prodCert, prodParams) = FakeRegister.ProductionIssued(ownerKey.PublicKey, 250);
        var allocationId = prodCert.Allocated(consCert, prodParams, consParams);
        _otherCertificate = prodCert;

        var @event = FakeRegister.CreateAllocationEvent(allocationId, prodCert.Id, consCert.Id, prodParams, prodParams);
        var transaction = FakeRegister.SignTransaction(@event.ConsumptionCertificateId, @event, ownerKey);

        var result = await _verifier.Verify(transaction, consCert, @event);

        AssertInvalid(result, "Slice not found");
    }

    [Fact]
    public async Task Verifier_InvalidSignatureForSlice_Invalid()
    {
        var ownerKey = _algorithm.Create();
        var otherKey = _algorithm.Create();
        var (consCert, consParams) = FakeRegister.ConsumptionIssued(ownerKey.PublicKey, 250);
        var (prodCert, prodParams) = FakeRegister.ProductionIssued(ownerKey.PublicKey, 250);
        var allocationId = prodCert.Allocated(consCert, prodParams, consParams);
        _otherCertificate = prodCert;

        var @event = FakeRegister.CreateAllocationEvent(allocationId, prodCert.Id, consCert.Id, prodParams, consParams);
        var transaction = FakeRegister.SignTransaction(@event.ConsumptionCertificateId, @event, otherKey);

        var result = await _verifier.Verify(transaction, consCert, @event);

        AssertInvalid(result, "Invalid signature for slice");
    }

    [Fact]
    public async Task Verifier_ProductionNotFound_Invalid()
    {
        var ownerKey = _algorithm.Create();
        var (consCert, consParams) = FakeRegister.ConsumptionIssued(ownerKey.PublicKey, 250);
        var (prodCert, prodParams) = FakeRegister.ProductionIssued(ownerKey.PublicKey, 250);
        var allocationId = prodCert.Allocated(consCert, prodParams, consParams);
        _otherCertificate = null;

        var @event = FakeRegister.CreateAllocationEvent(allocationId, prodCert.Id, consCert.Id, prodParams, consParams);
        var transaction = FakeRegister.SignTransaction(@event.ConsumptionCertificateId, @event, ownerKey);

        var result = await _verifier.Verify(transaction, consCert, @event);

        AssertInvalid(result, "ProductionCertificate does not exist");
    }

    [Fact]
    public async Task Verifier_ProdNotAllocated_Invalid()
    {
        var ownerKey = _algorithm.Create();
        var (consCert, consParams) = FakeRegister.ConsumptionIssued(ownerKey.PublicKey, 250);
        var (prodCert, prodParams) = FakeRegister.ProductionIssued(ownerKey.PublicKey, 250);
        _otherCertificate = prodCert;

        var @event = FakeRegister.CreateAllocationEvent(Guid.NewGuid(), prodCert.Id, consCert.Id, prodParams, consParams);
        var transaction = FakeRegister.SignTransaction(@event.ConsumptionCertificateId, @event, ownerKey);

        var result = await _verifier.Verify(transaction, consCert, @event);

        AssertInvalid(result, "Production not allocated");
    }
}
