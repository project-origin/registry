using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoFixture;
using Microsoft.Extensions.Options;
using Moq;
using ProjectOrigin.Electricity.Consumption;
using ProjectOrigin.Electricity.Consumption.Verifiers;
using ProjectOrigin.Electricity.Interfaces;
using ProjectOrigin.Electricity.Models;
using ProjectOrigin.WalletSystem.Server.HDWallet;
using Xunit;

namespace ProjectOrigin.Electricity.Tests;

public class ConsumptionIssuedVerifierTests : AssertExtensions
{
    const string IssuerArea = "DK1";
    private IKeyAlgorithm _algorithm;
    private IPrivateKey _issuerKey;
    private ConsumptionIssuedVerifier _verifier;

    public ConsumptionIssuedVerifierTests()
    {
        _algorithm = new Secp256k1Algorithm();
        _issuerKey = _algorithm.Create();

        var optionsMock = new Mock<IOptions<IssuerOptions>>();
        optionsMock.Setup(obj => obj.Value).Returns(new IssuerOptions(_algorithm)
        {
            Issuers = new Dictionary<string, string>(){
                {IssuerArea, Convert.ToBase64String(_issuerKey.PublicKey.Export())},
            }
        });

        _verifier = new ConsumptionIssuedVerifier(optionsMock.Object, _algorithm);
    }

    [Fact]
    public async Task ConsumptionIssuedVerifier_IssueCertificate_Success()
    {
        var @event = FakeRegister.CreateConsumptionIssuedEvent();
        var transaction = FakeRegister.SignTransaction(@event.CertificateId, @event, _issuerKey);

        var result = await _verifier.Verify(transaction, null, @event);

        AssertValid(result);
    }

    [Fact]
    public async Task ConsumptionIssuedVerifier_CertificateExists_Fail()
    {
        var @event = FakeRegister.CreateConsumptionIssuedEvent();
        var transaction = FakeRegister.SignTransaction(@event.CertificateId, @event, _issuerKey);
        var certificate = new ConsumptionCertificate(@event, _algorithm);

        var result = await _verifier.Verify(transaction, certificate, @event);

        AssertInvalid(result, $"Certificate with id ”{@event.CertificateId.StreamId}” already exists");
    }

    [Fact]
    public async Task ConsumptionIssuedVerifier_QuantityCommitmentInvalid_Fail()
    {
        var @event = FakeRegister.CreateConsumptionIssuedEvent(quantityCommitmentOverride: FakeRegister.InvalidCommitment());
        var transaction = FakeRegister.SignTransaction(@event.CertificateId, @event, _issuerKey);

        var result = await _verifier.Verify(transaction, null, @event);

        AssertInvalid(result, "Invalid range proof for Quantity commitment");
    }

    [Fact]
    public async Task ConsumptionIssuedVerifier_InvalidOwner_Fail()
    {
        var randomOwnerKeyData = new V1.PublicKey
        {
            Content = Google.Protobuf.ByteString.CopyFrom(new Fixture().Create<byte[]>())
        };

        var @event = FakeRegister.CreateConsumptionIssuedEvent(ownerKeyOverride: randomOwnerKeyData);
        var transaction = FakeRegister.SignTransaction(@event.CertificateId, @event, _issuerKey);

        var result = await _verifier.Verify(transaction, null, @event);

        AssertInvalid(result, "Invalid owner key, not a valid publicKey");
    }

    [Fact]
    public async Task ConsumptionIssuedVerifier_InvalidSignature_Fail()
    {
        var someOtherKey = _algorithm.Create();

        var @event = FakeRegister.CreateConsumptionIssuedEvent();
        var transaction = FakeRegister.SignTransaction(@event.CertificateId, @event, someOtherKey);

        var result = await _verifier.Verify(transaction, null, @event);

        AssertInvalid(result, "Invalid issuer signature for GridArea ”DK1”");
    }

    [Fact]
    public async Task ConsumptionIssuedVerifier_NoIssuerForArea_Fail()
    {
        var someOtherKey = _algorithm.Create();

        var @event = FakeRegister.CreateConsumptionIssuedEvent(gridAreaOverride: "DK2");
        var transaction = FakeRegister.SignTransaction(@event.CertificateId, @event, someOtherKey);

        var result = await _verifier.Verify(transaction, null, @event);

        AssertInvalid(result, "No issuer found for GridArea ”DK2”");
    }
}
