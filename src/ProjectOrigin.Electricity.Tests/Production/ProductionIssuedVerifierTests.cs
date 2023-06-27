using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using AutoFixture;
using Microsoft.Extensions.Options;
using Moq;
using ProjectOrigin.Electricity.Extensions;
using ProjectOrigin.Electricity.Models;
using ProjectOrigin.Electricity.Server.Options;
using ProjectOrigin.Electricity.Server.Services;
using ProjectOrigin.Electricity.Server.Verifiers;
using ProjectOrigin.HierarchicalDeterministicKeys;
using ProjectOrigin.HierarchicalDeterministicKeys.Interfaces;
using ProjectOrigin.PedersenCommitment;
using Xunit;

namespace ProjectOrigin.Electricity.Tests;

public class ProductionIssuedVerifierTests
{
    const string IssuerArea = "DK1";
    private IPrivateKey _issuerKey;
    private IssuedEventVerifier _verifier;

    public ProductionIssuedVerifierTests()
    {
        _issuerKey = Algorithms.Ed25519.GenerateNewPrivateKey();

        var optionsMock = new Mock<IOptions<IssuerOptions>>();
        optionsMock.Setup(obj => obj.Value).Returns(new IssuerOptions()
        {
            Issuers = new Dictionary<string, string>(){
                {IssuerArea, Convert.ToBase64String(Encoding.UTF8.GetBytes(_issuerKey.PublicKey.ExportPkixText()))},
            }
        });
        var issuerService = new GridAreaIssuerOptionsService(optionsMock.Object);

        _verifier = new IssuedEventVerifier(issuerService);
    }

    [Fact]
    public async Task ProductionIssuedVerifier_IssueCertificate_Success()
    {
        var @event = FakeRegister.CreateProductionIssuedEvent();
        var transaction = FakeRegister.SignTransaction(@event.CertificateId, @event, _issuerKey);

        var result = await _verifier.Verify(transaction, null, @event);

        result.AssertValid();
    }

    [Fact]
    public async Task ProductionIssuedVerifier_IssueCertificateWithPublicQuantity_Success()
    {
        var @event = FakeRegister.CreateProductionIssuedEvent(publicQuantity: true);
        var transaction = FakeRegister.SignTransaction(@event.CertificateId, @event, _issuerKey);

        var a = transaction.IsSignatureValid(_issuerKey.PublicKey);

        var result = await _verifier.Verify(transaction, null, @event);

        result.AssertValid();
    }

    [Fact]
    public async Task ProductionIssuedVerifier_CertificateExists_Fail()
    {
        var @event = FakeRegister.CreateProductionIssuedEvent();
        var transaction = FakeRegister.SignTransaction(@event.CertificateId, @event, _issuerKey);

        var result = await _verifier.Verify(transaction, new GranularCertificate(@event), @event);

        result.AssertInvalid($"Certificate with id ”{@event.CertificateId.StreamId}” already exists");
    }

    [Fact]
    public async Task ProductionIssuedVerifier_QuantityCommitmentInvalid_Fail()
    {
        var @event = FakeRegister.CreateProductionIssuedEvent(quantityCommitmentOverride: FakeRegister.InvalidCommitment());
        var transaction = FakeRegister.SignTransaction(@event.CertificateId, @event, _issuerKey);

        var result = await _verifier.Verify(transaction, null, @event);

        result.AssertInvalid("Invalid range proof for Quantity commitment");
    }

    // [Fact]
    // public async Task ProductionIssuedVerifier_InvalidPublicParameters_Fail()
    // {
    //     var @event = FakeRegister.CreateProductionIssuedEvent(publicQuantityCommitmentOverride: new SecretCommitmentInfo(695956), publicQuantity: true);
    //     var transaction = FakeRegister.SignTransaction(@event.CertificateId, @event, _issuerKey);

    //     var result = await _verifier.Verify(transaction, null, @event);

    //     result.AssertInvalid("Private and public quantity proof does not match");
    // }

    [Fact]
    public async Task ProductionIssuedVerifier_InvalidOwner_Fail()
    {
        var randomOwnerKeyData = new V1.PublicKey
        {
            Content = Google.Protobuf.ByteString.CopyFrom(new Fixture().Create<byte[]>())
        };

        var @event = FakeRegister.CreateProductionIssuedEvent(ownerKeyOverride: randomOwnerKeyData);
        var transaction = FakeRegister.SignTransaction(@event.CertificateId, @event, _issuerKey);

        var result = await _verifier.Verify(transaction, null, @event);

        result.AssertInvalid("Invalid owner key, not a valid publicKey");
    }

    [Fact]
    public async Task ProductionIssuedVerifier_InvalidSignature_Fail()
    {
        var invalidKey = Algorithms.Ed25519.GenerateNewPrivateKey();

        var @event = FakeRegister.CreateProductionIssuedEvent();
        var transaction = FakeRegister.SignTransaction(@event.CertificateId, @event, invalidKey);

        var result = await _verifier.Verify(transaction, null, @event);

        result.AssertInvalid("Invalid issuer signature for GridArea ”DK1”");
    }

    [Fact]
    public async Task ProductionIssuedVerifier_NoIssuerForArea_Fail()
    {
        var @event = FakeRegister.CreateProductionIssuedEvent(gridAreaOverride: "DK2");
        var transaction = FakeRegister.SignTransaction(@event.CertificateId, @event, _issuerKey);

        var result = await _verifier.Verify(transaction, null, @event);

        result.AssertInvalid("No issuer found for GridArea ”DK2”");
    }
}
