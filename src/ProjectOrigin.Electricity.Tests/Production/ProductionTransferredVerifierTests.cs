using System.Threading.Tasks;
using AutoFixture;
using ProjectOrigin.Electricity.Production.Verifiers;
using ProjectOrigin.HierarchicalDeterministicKeys.Implementations;
using ProjectOrigin.HierarchicalDeterministicKeys.Interfaces;
using ProjectOrigin.PedersenCommitment;
using Xunit;

namespace ProjectOrigin.Electricity.Tests;

public class ProductionTransferredVerifierTests : AssertExtensions
{
    private IHDAlgorithm _algorithm;
    private ProductionTransferredVerifier _verifier;

    public ProductionTransferredVerifierTests()
    {
        _algorithm = new Secp256k1Algorithm();
        _verifier = new ProductionTransferredVerifier(_algorithm);
    }

    [Fact]
    public async Task ProductionTransferredVerifierTests_TransferCertificate_Valid()
    {
        var ownerKey = _algorithm.GenerateNewPrivateKey();
        var newOwnerKey = _algorithm.GenerateNewPrivateKey();
        var (cert, sourceParams) = FakeRegister.ProductionIssued(ownerKey.PublicKey, 250);

        var @event = FakeRegister.CreateTransferEvent(cert, sourceParams, newOwnerKey.PublicKey.ToProto());
        var transaction = FakeRegister.SignTransaction(@event.CertificateId, @event, ownerKey);

        var result = await _verifier.Verify(transaction, cert, @event);

        AssertValid(result);
    }

    [Fact]
    public async Task ProductionTransferredVerifierTests_NullCertificate_Invalid()
    {
        var ownerKey = _algorithm.GenerateNewPrivateKey();
        var newOwnerKey = _algorithm.GenerateNewPrivateKey();
        var (cert, sourceParams) = FakeRegister.ProductionIssued(ownerKey.PublicKey, 250);

        var @event = FakeRegister.CreateTransferEvent(cert, sourceParams, newOwnerKey.PublicKey.ToProto());
        var transaction = FakeRegister.SignTransaction(@event.CertificateId, @event, ownerKey);

        var result = await _verifier.Verify(transaction, null, @event);

        AssertInvalid(result, "Certificate does not exist");
    }

    [Fact]
    public async Task ProductionTransferredVerifierTests_InvalidPublicKey_InvalidFormat()
    {
        var ownerKey = _algorithm.GenerateNewPrivateKey();
        var newOwnerKey = _algorithm.GenerateNewPrivateKey();
        var (cert, sourceParams) = FakeRegister.ProductionIssued(ownerKey.PublicKey, 250);

        var randomOwnerKeyData = new V1.PublicKey
        {
            Content = Google.Protobuf.ByteString.CopyFrom(new Fixture().Create<byte[]>())
        };

        var @event = FakeRegister.CreateTransferEvent(cert, sourceParams, randomOwnerKeyData);
        var transaction = FakeRegister.SignTransaction(@event.CertificateId, @event, ownerKey);

        var result = await _verifier.Verify(transaction, cert, @event);

        AssertInvalid(result, "Invalid NewOwner key, not a valid publicKey");
    }

    [Fact]
    public async Task ProductionTransferredVerifierTests_FakeSlice_SliceNotFound()
    {
        var ownerKey = _algorithm.GenerateNewPrivateKey();
        var newOwnerKey = _algorithm.GenerateNewPrivateKey();
        var (cert, sourceParams) = FakeRegister.ProductionIssued(ownerKey.PublicKey, 250);

        var fakeSliceParams = new SecretCommitmentInfo(250);
        var @event = FakeRegister.CreateTransferEvent(cert, fakeSliceParams, newOwnerKey.PublicKey.ToProto());
        var transaction = FakeRegister.SignTransaction(@event.CertificateId, @event, ownerKey);

        var result = await _verifier.Verify(transaction, cert, @event);

        AssertInvalid(result, "Slice not found");
    }

    [Fact]
    public async Task ProductionTransferredVerifierTests_WrongKey_InvalidSignature()
    {
        var ownerKey = _algorithm.GenerateNewPrivateKey();
        var newOwnerKey = _algorithm.GenerateNewPrivateKey();
        var (cert, sourceParams) = FakeRegister.ProductionIssued(ownerKey.PublicKey, 250);

        var @event = FakeRegister.CreateTransferEvent(cert, sourceParams, newOwnerKey.PublicKey.ToProto());
        var transaction = FakeRegister.SignTransaction(@event.CertificateId, @event, newOwnerKey);

        var result = await _verifier.Verify(transaction, cert, @event);

        AssertInvalid(result, "Invalid signature for slice");
    }
}
