using System;
using System.Linq;
using System.Threading.Tasks;
using AutoFixture;
using Moq;
using ProjectOrigin.Electricity.Consumption;
using ProjectOrigin.Electricity.Interfaces;
using ProjectOrigin.Electricity.Models;
using ProjectOrigin.Electricity.Production.Verifiers;
using ProjectOrigin.Registry.Utils.Interfaces;
using ProjectOrigin.WalletSystem.Server.HDWallet;
using Xunit;

namespace ProjectOrigin.Electricity.Tests;

public class ProductionAllocatedVerifierTests : AssertExtensions
{
    private IKeyAlgorithm _algorithm;
    private ProductionAllocatedVerifier _verifier;
    private ConsumptionCertificate? _otherCertificate;

    public ProductionAllocatedVerifierTests()
    {
        _algorithm = new Secp256k1Algorithm();

        var modelLoaderMock = new Mock<IRemoteModelLoader>();
        modelLoaderMock.Setup(obj => obj.GetModel<ConsumptionCertificate>(It.IsAny<Common.V1.FederatedStreamId>()))
            .Returns(() => Task.FromResult(_otherCertificate));

        _verifier = new ProductionAllocatedVerifier(modelLoaderMock.Object);
    }

    [Fact]
    public async Task Verifier_AllocateCertificate_Valid()
    {
        var ownerKey = _algorithm.Create();
        var (consCert, consParams) = FakeRegister.ConsumptionIssued(ownerKey.PublicKey, 250);
        var (prodCert, prodParams) = FakeRegister.ProductionIssued(ownerKey.PublicKey, 250);
        _otherCertificate = consCert;

        var @event = FakeRegister.CreateAllocationEvent(Guid.NewGuid(), prodCert.Id, consCert.Id, prodParams, consParams);
        var transaction = FakeRegister.SignTransaction(@event.ProductionCertificateId, @event, ownerKey);

        var result = await _verifier.Verify(transaction, prodCert, @event);

        AssertValid(result);
    }

    [Fact]
    public async Task Verifier_AllocateCertificate_ProdCertNotFould()
    {
        var ownerKey = _algorithm.Create();
        var (consCert, consParams) = FakeRegister.ConsumptionIssued(ownerKey.PublicKey, 250);
        var (prodCert, prodParams) = FakeRegister.ProductionIssued(ownerKey.PublicKey, 250);
        _otherCertificate = consCert;

        var @event = FakeRegister.CreateAllocationEvent(Guid.NewGuid(), prodCert.Id, consCert.Id, prodParams, consParams);
        var transaction = FakeRegister.SignTransaction(@event.ProductionCertificateId, @event, ownerKey);

        var result = await _verifier.Verify(transaction, null, @event);

        AssertInvalid(result, "Certificate does not exist");
    }

    [Fact]
    public async Task Verifier_InvalidProductionSlice_SliceNotFound()
    {
        var ownerKey = _algorithm.Create();
        var (consCert, consParams) = FakeRegister.ConsumptionIssued(ownerKey.PublicKey, 250);
        var (prodCert, prodParams) = FakeRegister.ProductionIssued(ownerKey.PublicKey, 250);
        _otherCertificate = consCert;

        var @event = FakeRegister.CreateAllocationEvent(Guid.NewGuid(), prodCert.Id, consCert.Id, consParams, consParams);
        var transaction = FakeRegister.SignTransaction(@event.ProductionCertificateId, @event, ownerKey);

        var result = await _verifier.Verify(transaction, prodCert, @event);

        AssertInvalid(result, "Production slice does not exist");
    }

    [Fact]
    public async Task Verifier_WrongKey_InvalidSignature()
    {
        var ownerKey = _algorithm.Create();
        var otherKey = _algorithm.Create();
        var (consCert, consParams) = FakeRegister.ConsumptionIssued(ownerKey.PublicKey, 250);
        var (prodCert, prodParams) = FakeRegister.ProductionIssued(ownerKey.PublicKey, 250);
        _otherCertificate = consCert;

        var @event = FakeRegister.CreateAllocationEvent(Guid.NewGuid(), prodCert.Id, consCert.Id, prodParams, consParams);
        var transaction = FakeRegister.SignTransaction(@event.ProductionCertificateId, @event, otherKey);

        var result = await _verifier.Verify(transaction, prodCert, @event);

        AssertInvalid(result, "Invalid signature for slice");
    }

    [Fact]
    public async Task Verifier_AllocateCertificate_ConsCertNotFould()
    {
        var ownerKey = _algorithm.Create();
        var (consCert, consParams) = FakeRegister.ConsumptionIssued(ownerKey.PublicKey, 250);
        var (prodCert, prodParams) = FakeRegister.ProductionIssued(ownerKey.PublicKey, 250);
        _otherCertificate = null;

        var @event = FakeRegister.CreateAllocationEvent(Guid.NewGuid(), prodCert.Id, consCert.Id, prodParams, consParams);
        var transaction = FakeRegister.SignTransaction(@event.ProductionCertificateId, @event, ownerKey);

        var result = await _verifier.Verify(transaction, prodCert, @event);

        AssertInvalid(result, "ConsumptionCertificate does not exist");
    }

    [Fact]
    public async Task Verifier_AllocateCertificate_InvalidPeriod()
    {
        var ownerKey = _algorithm.Create();
        var (consCert, consParams) = FakeRegister.ConsumptionIssued(ownerKey.PublicKey, 250);
        var hourLater = new DateInterval(consCert.Period.Start, consCert.Period.End.AddHours(1));
        var (prodCert, prodParams) = FakeRegister.ProductionIssued(ownerKey.PublicKey, 250, periodOverride: hourLater);
        _otherCertificate = consCert;

        var @event = FakeRegister.CreateAllocationEvent(Guid.NewGuid(), prodCert.Id, consCert.Id, prodParams, consParams);
        var transaction = FakeRegister.SignTransaction(@event.ProductionCertificateId, @event, ownerKey);

        var result = await _verifier.Verify(transaction, prodCert, @event);

        AssertInvalid(result, "Certificates are not in the same period");
    }

    [Fact]
    public async Task Verifier_AllocateCertificate_InvalidArea()
    {
        var ownerKey = _algorithm.Create();
        var (consCert, consParams) = FakeRegister.ConsumptionIssued(ownerKey.PublicKey, 250, area: "DK1");
        var (prodCert, prodParams) = FakeRegister.ProductionIssued(ownerKey.PublicKey, 250, area: "DK2");
        _otherCertificate = consCert;

        var @event = FakeRegister.CreateAllocationEvent(Guid.NewGuid(), prodCert.Id, consCert.Id, prodParams, consParams);
        var transaction = FakeRegister.SignTransaction(@event.ProductionCertificateId, @event, ownerKey);

        var result = await _verifier.Verify(transaction, prodCert, @event);

        AssertInvalid(result, "Certificates are not in the same area");
    }

    [Fact]
    public async Task Verifier_WrongConsumptionSlice_SliceNotFound()
    {
        var ownerKey = _algorithm.Create();
        var (consCert, consParams) = FakeRegister.ConsumptionIssued(ownerKey.PublicKey, 250);
        var (prodCert, prodParams) = FakeRegister.ProductionIssued(ownerKey.PublicKey, 250);
        _otherCertificate = consCert;

        var @event = FakeRegister.CreateAllocationEvent(Guid.NewGuid(), prodCert.Id, consCert.Id, prodParams, prodParams);
        var transaction = FakeRegister.SignTransaction(@event.ProductionCertificateId, @event, ownerKey);

        var result = await _verifier.Verify(transaction, prodCert, @event);

        AssertInvalid(result, "Consumption slice does not exist");
    }

    [Fact]
    public async Task Verifier_RandomProofData_InvalidEqualityProof()
    {
        var ownerKey = _algorithm.Create();
        var (consCert, consParams) = FakeRegister.ConsumptionIssued(ownerKey.PublicKey, 250);
        var (prodCert, prodParams) = FakeRegister.ProductionIssued(ownerKey.PublicKey, 250);
        _otherCertificate = consCert;

        var @event = FakeRegister.CreateAllocationEvent(Guid.NewGuid(), prodCert.Id, consCert.Id, prodParams, consParams, overwrideEqualityProof: new Fixture().CreateMany<byte>(64).ToArray());
        var transaction = FakeRegister.SignTransaction(@event.ProductionCertificateId, @event, ownerKey);

        var result = await _verifier.Verify(transaction, prodCert, @event);

        AssertInvalid(result, "Invalid Equality proof");
    }
}
