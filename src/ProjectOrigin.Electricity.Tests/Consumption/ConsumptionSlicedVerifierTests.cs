using Google.Protobuf;
using Microsoft.Extensions.Options;
using NSec.Cryptography;
using ProjectOrigin.Electricity.Consumption;
using ProjectOrigin.Electricity.Interfaces;
using ProjectOrigin.Electricity.Production.Requests;
using ProjectOrigin.PedersenCommitment;
using ProjectOrigin.Register.StepProcessor.Models;

namespace ProjectOrigin.Electricity.Tests;

public class ConsumptionSlicedVerifierTests
{
    private IOptions<T> CreateOptionsMock<T>(T content) where T : class
    {
        var optionsMock = new Mock<IOptions<T>>();
        optionsMock.Setup(obj => obj.Value).Returns(content);
        return optionsMock.Object;
    }

    private Group Group { get => FakeRegister.Group; }

    private ConsumptionSlicedEventVerifier Verifier { get => new ConsumptionSlicedEventVerifier(); }

    [Fact]
    public async Task ConsumptionSlicedEventVerifier_TransferCertificate_Valid()
    {
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var (cert, sourceParams) = FakeRegister.ConsumptionIssued(ownerKey.PublicKey, 250);

        var request = FakeRegister.CreateSlices(cert, sourceParams, 150, ownerKey);

        var result = await Verifier.Verify(request);

        Assert.IsType<VerificationResult.Valid>(result);
    }

    [Fact]
    public async Task ConsumptionSlicedEventVerifier_NoCertificate_Invalid()
    {
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var (cert, sourceParams) = FakeRegister.ConsumptionIssued(ownerKey.PublicKey, 250);

        var request = FakeRegister.CreateSlices(cert, sourceParams, 150, ownerKey);
        var modifiedRequest = new VerificationRequest<ConsumptionCertificate, V1.SlicedEvent>(
            null,
            request.Event,
            request.Signature,
            request.AdditionalStreams
        );

        var result = await Verifier.Verify(modifiedRequest);

        var invalid = Assert.IsType<VerificationResult.Invalid>(result);
        Assert.Equal($"Certificate does not exist", invalid!.ErrorMessage);
    }

    [Fact]
    public async Task ConsumptionSlicedEventVerifier_FakeSlice_SliceNotFound()
    {
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var newOwnerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var (cert, sourceParams) = FakeRegister.ConsumptionIssued(ownerKey.PublicKey, 250);

        var fakeSliceParams = FakeRegister.Group.Commit(250);
        var request = FakeRegister.CreateSlices(cert, fakeSliceParams, 150, ownerKey);

        var result = await Verifier.Verify(request);

        var invalid = Assert.IsType<VerificationResult.Invalid>(result);
        Assert.Equal("Slice not found", invalid!.ErrorMessage);
    }

    [Fact]
    public async Task ConsumptionSlicedEventVerifier_WrongKey_InvalidSignature()
    {
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var otherKey = Key.Create(SignatureAlgorithm.Ed25519);
        var (cert, sourceParams) = FakeRegister.ConsumptionIssued(ownerKey.PublicKey, 250);

        var request = FakeRegister.CreateSlices(cert, sourceParams, 150, otherKey);

        var result = await Verifier.Verify(request);

        var invalid = Assert.IsType<VerificationResult.Invalid>(result);
        Assert.Equal("Invalid signature for slice", invalid!.ErrorMessage);
    }


    [Fact]
    public async Task ConsumptionSlicedEventVerifier_InvalidSlicePublicKey_InvalidFormat()
    {
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var (cert, sourceParams) = FakeRegister.ConsumptionIssued(ownerKey.PublicKey, 250);

        var randomOwnerKeyData = new V1.PublicKey
        {
            Content = Google.Protobuf.ByteString.CopyFrom(new Fixture().Create<byte[]>())
        };

        var request = FakeRegister.CreateSlices(cert, sourceParams, 150, ownerKey, randomOwnerKeyData);

        var result = await Verifier.Verify(request);

        var invalid = Assert.IsType<VerificationResult.Invalid>(result);
        Assert.Equal("Invalid NewOwner key, not a valid Ed25519 publicKey", invalid!.ErrorMessage);
    }

    [Fact]
    public async Task ConsumptionSlicedEventVerifier_InvalidSumProof_Invalid()
    {
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var (cert, sourceParams) = FakeRegister.ConsumptionIssued(ownerKey.PublicKey, 250);

        var sumOverride = ByteString.CopyFrom(new Fixture().Create<byte[]>());

        var request = FakeRegister.CreateSlices(cert, sourceParams, 150, ownerKey, sumOverride: sumOverride);

        var result = await Verifier.Verify(request);

        var invalid = Assert.IsType<VerificationResult.Invalid>(result);
        Assert.Equal("Invalid sum proof", invalid!.ErrorMessage);
    }
}
