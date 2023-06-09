using System.Linq;
using System.Threading.Tasks;
using AutoFixture;
using Google.Protobuf;
using ProjectOrigin.Electricity.Consumption.Verifiers;
using ProjectOrigin.Electricity.Interfaces;
using ProjectOrigin.PedersenCommitment;
using ProjectOrigin.WalletSystem.Server.HDWallet;
using Xunit;

namespace ProjectOrigin.Electricity.Tests;

public class ConsumptionSlicedVerifierTests : AssertExtensions
{
    private IKeyAlgorithm _algorithm;
    private ConsumptionSlicedVerifier _verifier;

    public ConsumptionSlicedVerifierTests()
    {
        _algorithm = new Secp256k1Algorithm();
        _verifier = new ConsumptionSlicedVerifier(_algorithm);
    }

    [Fact]
    public async Task ConsumptionSlicedEventVerifier_TransferCertificate_Valid()
    {
        var ownerKey = _algorithm.Create();
        var (cert, sourceParams) = FakeRegister.ConsumptionIssued(ownerKey.PublicKey, 250);

        var @event = FakeRegister.CreateSliceEvent(cert.Id, sourceParams, 150, ownerKey.PublicKey);
        var transaction = FakeRegister.SignTransaction(@event.CertificateId, @event, ownerKey);

        var result = await _verifier.Verify(transaction, cert, @event);

        AssertValid(result);
    }

    [Fact]
    public async Task ConsumptionSlicedEventVerifier_NoCertificate_Invalid()
    {
        var ownerKey = _algorithm.Create();
        var (cert, sourceParams) = FakeRegister.ConsumptionIssued(ownerKey.PublicKey, 250);

        var @event = FakeRegister.CreateSliceEvent(cert.Id, sourceParams, 150, ownerKey.PublicKey);
        var transaction = FakeRegister.SignTransaction(@event.CertificateId, @event, ownerKey);

        var result = await _verifier.Verify(transaction, null, @event);

        AssertInvalid(result, "Certificate does not exist");
    }

    [Fact]
    public async Task ConsumptionSlicedEventVerifier_FakeSlice_SliceNotFound()
    {
        var ownerKey = _algorithm.Create();
        var newOwnerKey = _algorithm.Create();
        var (cert, sourceParams) = FakeRegister.ConsumptionIssued(ownerKey.PublicKey, 250);

        var fakeSliceParams = new SecretCommitmentInfo(250);
        var @event = FakeRegister.CreateSliceEvent(cert.Id, fakeSliceParams, 150, ownerKey.PublicKey);
        var transaction = FakeRegister.SignTransaction(@event.CertificateId, @event, ownerKey);

        var result = await _verifier.Verify(transaction, cert, @event);

        AssertInvalid(result, "Slice not found");
    }

    [Fact]
    public async Task ConsumptionSlicedEventVerifier_WrongKey_InvalidSignature()
    {
        var ownerKey = _algorithm.Create();
        var otherKey = _algorithm.Create();
        var (cert, sourceParams) = FakeRegister.ConsumptionIssued(ownerKey.PublicKey, 250);

        var @event = FakeRegister.CreateSliceEvent(cert.Id, sourceParams, 150, otherKey.PublicKey);
        var transaction = FakeRegister.SignTransaction(@event.CertificateId, @event, otherKey);

        var result = await _verifier.Verify(transaction, cert, @event);

        AssertInvalid(result, "Invalid signature for slice");
    }

    [Fact]
    public async Task ConsumptionSlicedEventVerifier_InvalidSlicePublicKey_InvalidFormat()
    {
        var ownerKey = _algorithm.Create();
        var (cert, sourceParams) = FakeRegister.ConsumptionIssued(ownerKey.PublicKey, 250);

        var randomOwnerKeyData = new V1.PublicKey
        {
            Content = Google.Protobuf.ByteString.CopyFrom(new Fixture().Create<byte[]>())
        };

        var @event = FakeRegister.CreateSliceEvent(cert.Id, sourceParams, 150, ownerKey.PublicKey, randomOwnerKeyData);
        var transaction = FakeRegister.SignTransaction(@event.CertificateId, @event, ownerKey);

        var result = await _verifier.Verify(transaction, cert, @event);

        AssertInvalid(result, "Invalid NewOwner key, not a valid publicKey");
    }

    [Fact]
    public async Task ConsumptionSlicedEventVerifier_InvalidSumProof_Invalid()
    {
        var ownerKey = _algorithm.Create();
        var (cert, sourceParams) = FakeRegister.ConsumptionIssued(ownerKey.PublicKey, 250);

        var sumOverride = ByteString.CopyFrom(new Fixture().CreateMany<byte>(64).ToArray());

        var @event = FakeRegister.CreateSliceEvent(cert.Id, sourceParams, 150, ownerKey.PublicKey, sumOverride: sumOverride);
        var transaction = FakeRegister.SignTransaction(@event.CertificateId, @event, ownerKey);

        var result = await _verifier.Verify(transaction, cert, @event);

        AssertInvalid(result, "Invalid sum proof");
    }
}
