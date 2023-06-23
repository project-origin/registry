using System.Threading.Tasks;
using AutoFixture;
using ProjectOrigin.Electricity.Production.Verifiers;
using ProjectOrigin.HierarchicalDeterministicKeys;
using ProjectOrigin.PedersenCommitment;
using Xunit;

namespace ProjectOrigin.Electricity.Tests;

public class ProductionTransferredVerifierTests
{
    private ProductionTransferredVerifier _verifier;

    public ProductionTransferredVerifierTests()
    {
        _verifier = new ProductionTransferredVerifier();
    }

    [Fact]
    public async Task ProductionTransferredVerifierTests_TransferCertificate_Valid()
    {
        var ownerKey = Algorithms.Secp256k1.GenerateNewPrivateKey();
        var newOwnerKey = Algorithms.Secp256k1.GenerateNewPrivateKey();
        var (cert, sourceParams) = FakeRegister.ProductionIssued(ownerKey.PublicKey, 250);

        var @event = FakeRegister.CreateTransferEvent(cert, sourceParams, newOwnerKey.PublicKey.ToProto());
        var transaction = FakeRegister.SignTransaction(@event.CertificateId, @event, ownerKey);

        var result = await _verifier.Verify(transaction, cert, @event);

        result.AssertValid();
    }

    [Fact]
    public async Task ProductionTransferredVerifierTests_NullCertificate_Invalid()
    {
        var ownerKey = Algorithms.Secp256k1.GenerateNewPrivateKey();
        var newOwnerKey = Algorithms.Secp256k1.GenerateNewPrivateKey();
        var (cert, sourceParams) = FakeRegister.ProductionIssued(ownerKey.PublicKey, 250);

        var @event = FakeRegister.CreateTransferEvent(cert, sourceParams, newOwnerKey.PublicKey.ToProto());
        var transaction = FakeRegister.SignTransaction(@event.CertificateId, @event, ownerKey);

        var result = await _verifier.Verify(transaction, null, @event);

        result.AssertInvalid("Certificate does not exist");
    }

    [Fact]
    public async Task ProductionTransferredVerifierTests_InvalidPublicKey_InvalidFormat()
    {
        var ownerKey = Algorithms.Secp256k1.GenerateNewPrivateKey();
        var newOwnerKey = Algorithms.Secp256k1.GenerateNewPrivateKey();
        var (cert, sourceParams) = FakeRegister.ProductionIssued(ownerKey.PublicKey, 250);

        var randomOwnerKeyData = new V1.PublicKey
        {
            Content = Google.Protobuf.ByteString.CopyFrom(new Fixture().Create<byte[]>())
        };

        var @event = FakeRegister.CreateTransferEvent(cert, sourceParams, randomOwnerKeyData);
        var transaction = FakeRegister.SignTransaction(@event.CertificateId, @event, ownerKey);

        var result = await _verifier.Verify(transaction, cert, @event);

        result.AssertInvalid("Invalid NewOwner key, not a valid publicKey");
    }

    [Fact]
    public async Task ProductionTransferredVerifierTests_FakeSlice_SliceNotFound()
    {
        var ownerKey = Algorithms.Secp256k1.GenerateNewPrivateKey();
        var newOwnerKey = Algorithms.Secp256k1.GenerateNewPrivateKey();
        var (cert, sourceParams) = FakeRegister.ProductionIssued(ownerKey.PublicKey, 250);

        var fakeSliceParams = new SecretCommitmentInfo(250);
        var @event = FakeRegister.CreateTransferEvent(cert, fakeSliceParams, newOwnerKey.PublicKey.ToProto());
        var transaction = FakeRegister.SignTransaction(@event.CertificateId, @event, ownerKey);

        var result = await _verifier.Verify(transaction, cert, @event);

        result.AssertInvalid("Slice not found");
    }

    [Fact]
    public async Task ProductionTransferredVerifierTests_WrongKey_InvalidSignature()
    {
        var ownerKey = Algorithms.Secp256k1.GenerateNewPrivateKey();
        var newOwnerKey = Algorithms.Secp256k1.GenerateNewPrivateKey();
        var (cert, sourceParams) = FakeRegister.ProductionIssued(ownerKey.PublicKey, 250);

        var @event = FakeRegister.CreateTransferEvent(cert, sourceParams, newOwnerKey.PublicKey.ToProto());
        var transaction = FakeRegister.SignTransaction(@event.CertificateId, @event, newOwnerKey);

        var result = await _verifier.Verify(transaction, cert, @event);

        result.AssertInvalid("Invalid signature for slice");
    }
}
