using System.Numerics;
using Microsoft.Extensions.Options;
using NSec.Cryptography;
using ProjectOrigin.Electricity.Production;
using ProjectOrigin.Electricity.Production.Requests;
using ProjectOrigin.Electricity.Shared;
using ProjectOrigin.Electricity.Shared.Internal;
using ProjectOrigin.PedersenCommitment;
using ProjectOrigin.RequestProcessor.Interfaces;
using ProjectOrigin.RequestProcessor.Services;

namespace ProjectOrigin.Electricity.Tests;

public class ProductionSliceTransferredVerifierTests
{
    private IEventSerializer serializer = new JsonEventSerializer();

    private IOptions<T> CreateOptionsMock<T>(T content) where T : class
    {
        var optionsMock = new Mock<IOptions<T>>();
        optionsMock.Setup(obj => obj.Value).Returns(content);
        return optionsMock.Object;
    }

    private Lazy<Group> lazyGroup = new Lazy<Group>(() => Group.Create(), true);

    private Group Group { get => lazyGroup.Value; }
    private ProductionSliceTransferredVerifier Verifier { get => new ProductionSliceTransferredVerifier(serializer); }

    [Fact]
    public async Task ProductionSliceTransferredVerifier_TransferCertificate_Valid()
    {
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var (cert, sourceParams) = IssueProduction(ownerKey, 250);

        var request = CreateTransfer(cert.Id, sourceParams, 150, ownerKey);

        var result = await Verifier.Verify(request, cert);

        Assert.True(result.IsValid, result.ErrorMessage);
    }

    [Fact]
    public async Task ProductionSliceTransferredVerifier_TransferCertificateNested_Valid()
    {
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var otherOwner = Key.Create(SignatureAlgorithm.Ed25519);
        var (cert, sourceParams) = IssueProduction(ownerKey, 250);

        var (transferParams, remainderParams) = Transfer(cert, sourceParams, 150, otherOwner.PublicKey);

        var request = CreateTransfer(cert.Id, transferParams, 150, signerKey: otherOwner);

        var result = await Verifier.Verify(request, cert);

        Assert.True(result.IsValid, result.ErrorMessage);
    }

    [Fact]
    public async Task ProductionSliceTransferredVerifier_CertificateDontExist_Invalid()
    {
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var sourceParams = Group.Commit(200);

        var request = CreateTransfer(new("", Guid.NewGuid()), sourceParams, 150, ownerKey);

        var result = await Verifier.Verify(request, null);

        Assert.False(result.IsValid);
        Assert.Equal("Certificate does not exist", result.ErrorMessage);
    }

    [Fact]
    public async Task ProductionSliceTransferredVerifier_SourceInvalid_Invalid()
    {
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var (cert, sourceParams) = IssueProduction(ownerKey, 250);

        var request = CreateTransfer(cert.Id, sourceParams, 150, ownerKey, sourceParametersOverride: Group.Commit(250));

        var result = await Verifier.Verify(request, cert);

        Assert.False(result.IsValid);
        Assert.Equal("Calculated Source commitment does not equal the parameters", result.ErrorMessage);
    }

    [Fact]
    public async Task ProductionSliceTransferredVerifier_TransferredInvalid_Invalid()
    {
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var (cert, sourceParams) = IssueProduction(ownerKey, 250);

        var request = CreateTransfer(cert.Id, sourceParams, 150, ownerKey, transferParametersOverride: Group.Commit(150));

        var result = await Verifier.Verify(request, cert);

        Assert.False(result.IsValid);
        Assert.Equal("Calculated Transferred commitment does not equal the parameters", result.ErrorMessage);
    }

    [Fact]
    public async Task ProductionSliceTransferredVerifier_RemainderInvalid_Invalid()
    {
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var (cert, sourceParams) = IssueProduction(ownerKey, 250);

        var request = CreateTransfer(cert.Id, sourceParams, 150, ownerKey, remainderParametersOverride: Group.Commit(100));

        var result = await Verifier.Verify(request, cert);

        Assert.False(result.IsValid);
        Assert.Equal("Calculated Remainder commitment does not equal the parameters", result.ErrorMessage);
    }

    [Fact]
    public async Task ProductionSliceTransferredVerifier_NotCommitmentToZero_Invalid()
    {
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var (cert, sourceParams) = IssueProduction(ownerKey, 250);

        var request = CreateTransfer(cert.Id, sourceParams, 150, ownerKey, quantityOffset: 100);

        var result = await Verifier.Verify(request, cert);

        Assert.False(result.IsValid);
        Assert.Equal("R to zero is not valid", result.ErrorMessage);
    }

    [Fact]
    public async Task ProductionSliceTransferredVerifier_ZeroTransfer_Invalid()
    {
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var (cert, sourceParams) = IssueProduction(ownerKey, 250);

        var request = CreateTransfer(cert.Id, sourceParams, 0, ownerKey);

        var result = await Verifier.Verify(request, cert);

        Assert.False(result.IsValid);
        Assert.Equal("Negative or zero transfer not allowed", result.ErrorMessage);
    }

    //TODO fix tests
    [Fact(Skip = "C# does not allow negative exponents ”m” so test has to be rewritten to get around it to prove verifier rejects it.")]
    public async Task ProductionSliceTransferredVerifier_NegativeTransfer_Invalid()
    {
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var (cert, sourceParams) = IssueProduction(ownerKey, 250);

        var request = CreateTransfer(cert.Id, sourceParams, -50, ownerKey);

        var result = await Verifier.Verify(request, cert);

        Assert.False(result.IsValid);
        Assert.Equal("Negative or zero transfer not allowed", result.ErrorMessage);
    }

    //TODO fix tests
    [Fact(Skip = "C# does not allow negative exponents ”m” so test has to be rewritten to get around it to prove verifier rejects it.")]
    public async Task ProductionSliceTransferredVerifier_LargerTransferThanQuantity_Invalid()
    {
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var (cert, sourceParams) = IssueProduction(ownerKey, 250);

        var request = CreateTransfer(cert.Id, sourceParams, 300, ownerKey);

        var result = await Verifier.Verify(request, cert);

        Assert.False(result.IsValid);
        Assert.Equal("Transfer larger than source", result.ErrorMessage);
    }

    [Fact]
    public async Task ProductionSliceTransferredVerifier_SliceNotFound_Invalid()
    {
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var (cert, sourceParams) = IssueProduction(ownerKey, 250);

        sourceParams = Group.Commit(250);

        var request = CreateTransfer(cert.Id, sourceParams, 150, ownerKey);

        var result = await Verifier.Verify(request, cert);

        Assert.False(result.IsValid);
        Assert.Equal("Slice not found", result.ErrorMessage);
    }

    [Fact]
    public async Task ProductionSliceTransferredVerifier_InvalidSignature_Invalid()
    {
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var (cert, sourceParams) = IssueProduction(ownerKey, 250);

        var otherKey = Key.Create(SignatureAlgorithm.Ed25519);
        var request = CreateTransfer(cert.Id, sourceParams, 150, otherKey);

        var result = await Verifier.Verify(request, cert);

        Assert.False(result.IsValid);
        Assert.Equal("Invalid signature", result.ErrorMessage);
    }

    private (CommitmentParameters transfer, CommitmentParameters remainder) Transfer(ProductionCertificate cert, CommitmentParameters sourceParams, long quantity, PublicKey newOwner)
    {
        var e = CreateTransferEvent(cert.Id, sourceParams, quantity, newOwner);

        cert.Apply(e.e);

        return (e.transfer, e.remainder);
    }

    private (ProductionCertificate, CommitmentParameters) IssueProduction(Key ownerKey, long quantity)
    {
        var quantityCommitmentParameters = Group.Commit(quantity);
        var gsrnCommitmentParameters = Group.Commit(new Fixture().Create<long>());

        var e = new ProductionIssuedEvent(
                new("", Guid.NewGuid()),
                new TimePeriod(
                    DateTimeOffset.Now,
                    DateTimeOffset.Now.AddHours(1)),
                "DK1",
                gsrnCommitmentParameters.Commitment,
                quantityCommitmentParameters.Commitment,
                "F01050100",
                "T020002",
                ownerKey.PublicKey.Export(KeyBlobFormat.RawPublicKey)
                );

        var cert = new ProductionCertificate();
        cert.Apply(e);

        return (cert, quantityCommitmentParameters);

    }

    private ProductionSliceTransferredRequest CreateTransfer(
        FederatedStreamId id,
        CommitmentParameters sourceParameters,
        long quantity,
        Key signerKey,
        CommitmentParameters? sourceParametersOverride = null,
        CommitmentParameters? transferParametersOverride = null,
        CommitmentParameters? remainderParametersOverride = null,
        long quantityOffset = 0
        )
    {
        var newOwner = Key.Create(SignatureAlgorithm.Ed25519).PublicKey;

        var (e, transferParamerters, remainderParameters) = CreateTransferEvent(id, sourceParameters, quantity, newOwner, quantityOffset);

        var serializedEvent = serializer.Serialize(e);
        var signature = NSec.Cryptography.Ed25519.Ed25519.Sign(signerKey, serializedEvent);

        var request = new ProductionSliceTransferredRequest(
            new SliceParameters(
                sourceParametersOverride ?? sourceParameters,
                transferParametersOverride ?? transferParamerters,
                remainderParametersOverride ?? remainderParameters
            ),
            Event: e,
            Signature: signature);

        return request;
    }

    private (ProductionSliceTransferredEvent e, CommitmentParameters transfer, CommitmentParameters remainder) CreateTransferEvent(FederatedStreamId id, CommitmentParameters sourceParameters, long quantity, PublicKey newOwner, long quantityOffset = 0)
    {
        var transferParameters = Group.Commit(quantity);
        var remainderParameters = Group.Commit(sourceParameters.m - quantity + quantityOffset);

        var e = new ProductionSliceTransferredEvent(
                id,
                new Slice(
                    sourceParameters.Commitment,
                    transferParameters.Commitment,
                    remainderParameters.Commitment,
                    (sourceParameters.r - (transferParameters.r + remainderParameters.r)).MathMod(Group.q)
                ),
                newOwner.Export(KeyBlobFormat.RawPublicKey)
                );

        return (e, transferParameters, remainderParameters);
    }
}
