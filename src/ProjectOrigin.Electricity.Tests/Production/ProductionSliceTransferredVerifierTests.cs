using Microsoft.Extensions.Options;
using NSec.Cryptography;
using ProjectOrigin.Electricity.Production.Requests;
using ProjectOrigin.PedersenCommitment;
using ProjectOrigin.Register.StepProcessor.Interfaces;
using ProjectOrigin.Register.StepProcessor.Models;
using ProjectOrigin.Register.StepProcessor.Services;

namespace ProjectOrigin.Electricity.Tests;

public class ProductionSliceTransferredVerifierTests
{
    private IOptions<T> CreateOptionsMock<T>(T content) where T : class
    {
        var optionsMock = new Mock<IOptions<T>>();
        optionsMock.Setup(obj => obj.Value).Returns(content);
        return optionsMock.Object;
    }

    private Group Group { get => FakeRegister.Group; }

    private ProductionSliceTransferredVerifier Verifier { get => new ProductionSliceTransferredVerifier(); }

    [Fact]
    public async Task ProductionSliceTransferredVerifier_TransferCertificate_Valid()
    {
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var (cert, sourceParams) = FakeRegister.ProductionIssued(ownerKey.PublicKey, 250);

        var request = FakeRegister.CreateTransfer(cert.Id, sourceParams, 150, ownerKey);

        var result = await Verifier.Verify(request, cert);

        Assert.IsType<VerificationResult.Valid>(result);
    }

    [Fact]
    public async Task ProductionSliceTransferredVerifier_TransferCertificate_InvalidNewOwner()
    {
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var (cert, sourceParams) = FakeRegister.ProductionIssued(ownerKey.PublicKey, 250);

        var request = FakeRegister.CreateTransfer(cert.Id, sourceParams, 150, ownerKey, newOwnerOverride: new byte[0]);

        var result = await Verifier.Verify(request, cert);

        var invalid = result as VerificationResult.Invalid;
        Assert.NotNull(invalid);
        Assert.Equal("Invalid NewOwner key, not a valid Ed25519 publicKey", invalid!.ErrorMessage);
    }

    [Fact]
    public async Task ProductionSliceTransferredVerifier_TransferCertificateNested_Valid()
    {
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var otherOwner = Key.Create(SignatureAlgorithm.Ed25519);
        var (cert, sourceParams) = FakeRegister.ProductionIssued(ownerKey.PublicKey, 250);

        var (transferParams, remainderParams) = cert.Transferred(sourceParams, 150, otherOwner.PublicKey);

        var request = FakeRegister.CreateTransfer(cert.Id, transferParams, 150, signerKey: otherOwner);

        var result = await Verifier.Verify(request, cert);

        Assert.IsType<VerificationResult.Valid>(result);
    }

    [Fact]
    public async Task ProductionSliceTransferredVerifier_CertificateDontExist_Invalid()
    {
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var sourceParams = Group.Commit(200);

        var request = FakeRegister.CreateTransfer(new("", Guid.NewGuid()), sourceParams, 150, ownerKey);

        var result = await Verifier.Verify(request, null);

        var invalid = result as VerificationResult.Invalid;
        Assert.NotNull(invalid);
        Assert.Equal("Certificate does not exist", invalid!.ErrorMessage);
    }

    [Fact]
    public async Task ProductionSliceTransferredVerifier_SourceInvalid_Invalid()
    {
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var (cert, sourceParams) = FakeRegister.ProductionIssued(ownerKey.PublicKey, 250);

        var request = FakeRegister.CreateTransfer(cert.Id, sourceParams, 150, ownerKey, sourceParametersOverride: Group.Commit(250));

        var result = await Verifier.Verify(request, cert);

        var invalid = result as VerificationResult.Invalid;
        Assert.NotNull(invalid);
        Assert.Equal("Calculated Source commitment does not equal the parameters", invalid!.ErrorMessage);
    }

    [Fact]
    public async Task ProductionSliceTransferredVerifier_TransferredInvalid_Invalid()
    {
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var (cert, sourceParams) = FakeRegister.ProductionIssued(ownerKey.PublicKey, 250);

        var request = FakeRegister.CreateTransfer(cert.Id, sourceParams, 150, ownerKey, transferParametersOverride: Group.Commit(150));

        var result = await Verifier.Verify(request, cert);

        var invalid = result as VerificationResult.Invalid;
        Assert.NotNull(invalid);
        Assert.Equal("Calculated Transferred commitment does not equal the parameters", invalid!.ErrorMessage);
    }

    [Fact]
    public async Task ProductionSliceTransferredVerifier_RemainderInvalid_Invalid()
    {
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var (cert, sourceParams) = FakeRegister.ProductionIssued(ownerKey.PublicKey, 250);

        var request = FakeRegister.CreateTransfer(cert.Id, sourceParams, 150, ownerKey, remainderParametersOverride: Group.Commit(100));

        var result = await Verifier.Verify(request, cert);

        var invalid = result as VerificationResult.Invalid;
        Assert.NotNull(invalid);
        Assert.Equal("Calculated Remainder commitment does not equal the parameters", invalid!.ErrorMessage);
    }

    [Fact]
    public async Task ProductionSliceTransferredVerifier_NotCommitmentToZero_Invalid()
    {
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var (cert, sourceParams) = FakeRegister.ProductionIssued(ownerKey.PublicKey, 250);

        var request = FakeRegister.CreateTransfer(cert.Id, sourceParams, 150, ownerKey, quantityOffset: 100);

        var result = await Verifier.Verify(request, cert);

        var invalid = result as VerificationResult.Invalid;
        Assert.NotNull(invalid);
        Assert.Equal("R to zero is not valid", invalid!.ErrorMessage);
    }

    [Fact]
    public async Task ProductionSliceTransferredVerifier_ZeroTransfer_Invalid()
    {
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var (cert, sourceParams) = FakeRegister.ProductionIssued(ownerKey.PublicKey, 250);

        var request = FakeRegister.CreateTransfer(cert.Id, sourceParams, 0, ownerKey);

        var result = await Verifier.Verify(request, cert);

        var invalid = result as VerificationResult.Invalid;
        Assert.NotNull(invalid);
        Assert.Equal("Negative or zero transfer not allowed", invalid!.ErrorMessage);
    }

    //TODO fix tests
    [Fact(Skip = "C# does not allow negative exponents ”m” so test has to be rewritten to get around it to prove verifier rejects it.")]
    public async Task ProductionSliceTransferredVerifier_NegativeTransfer_Invalid()
    {
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var (cert, sourceParams) = FakeRegister.ProductionIssued(ownerKey.PublicKey, 250);

        var request = FakeRegister.CreateTransfer(cert.Id, sourceParams, -50, ownerKey);

        var result = await Verifier.Verify(request, cert);

        var invalid = result as VerificationResult.Invalid;
        Assert.NotNull(invalid);
        Assert.Equal("Negative or zero transfer not allowed", invalid!.ErrorMessage);
    }

    //TODO fix tests
    [Fact(Skip = "C# does not allow negative exponents ”m” so test has to be rewritten to get around it to prove verifier rejects it.")]
    public async Task ProductionSliceTransferredVerifier_LargerTransferThanQuantity_Invalid()
    {
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var (cert, sourceParams) = FakeRegister.ProductionIssued(ownerKey.PublicKey, 250);

        var request = FakeRegister.CreateTransfer(cert.Id, sourceParams, 300, ownerKey);

        var result = await Verifier.Verify(request, cert);

        var invalid = result as VerificationResult.Invalid;
        Assert.NotNull(invalid);
        Assert.Equal("Transfer larger than source", invalid!.ErrorMessage);
    }

    [Fact]
    public async Task ProductionSliceTransferredVerifier_SliceNotFound_Invalid()
    {
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var (cert, sourceParams) = FakeRegister.ProductionIssued(ownerKey.PublicKey, 250);

        sourceParams = Group.Commit(250);

        var request = FakeRegister.CreateTransfer(cert.Id, sourceParams, 150, ownerKey);

        var result = await Verifier.Verify(request, cert);

        var invalid = result as VerificationResult.Invalid;
        Assert.NotNull(invalid);
        Assert.Equal("Slice not found", invalid!.ErrorMessage);
    }

    [Fact]
    public async Task ProductionSliceTransferredVerifier_InvalidSignature_Invalid()
    {
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var (cert, sourceParams) = FakeRegister.ProductionIssued(ownerKey.PublicKey, 250);

        var otherKey = Key.Create(SignatureAlgorithm.Ed25519);
        var request = FakeRegister.CreateTransfer(cert.Id, sourceParams, 150, otherKey);

        var result = await Verifier.Verify(request, cert);

        var invalid = result as VerificationResult.Invalid;
        Assert.NotNull(invalid);
        Assert.Equal("Invalid signature", invalid!.ErrorMessage);
    }
}
