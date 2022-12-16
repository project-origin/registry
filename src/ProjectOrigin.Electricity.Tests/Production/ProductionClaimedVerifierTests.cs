using NSec.Cryptography;
using ProjectOrigin.Electricity.Production.Verifiers;

namespace ProjectOrigin.Electricity.Tests;

public class ProductionClaimedVerifierTests : AbstractVerifierTests
{
    private ProductionClaimedVerifier Verifier { get => new ProductionClaimedVerifier(); }

    [Fact]
    public async Task ProductionClaimedVerifier_Valid()
    {
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var (consCert, consParams) = FakeRegister.ConsumptionIssued(ownerKey.PublicKey, 250);
        var (prodCert, prodParams) = FakeRegister.ProductionIssued(ownerKey.PublicKey, 250);
        var allocationId = prodCert.Allocated(consCert, prodParams, consParams, ownerKey);
        consCert.Allocated(allocationId, prodCert, prodParams, consParams, ownerKey);

        var request = FakeRegister.CreateProductionClaim(allocationId, prodCert, consCert, ownerKey);
        var result = await Verifier.Verify(request);

        AssertValid(result);
    }

    [Fact]
    public async Task ProductionClaimedVerifier_Invalid_CertificateNotExists()
    {
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var (consCert, consParams) = FakeRegister.ConsumptionIssued(ownerKey.PublicKey, 250);
        var (prodCert, prodParams) = FakeRegister.ProductionIssued(ownerKey.PublicKey, 250);
        var allocationId = prodCert.Allocated(consCert, prodParams, consParams, ownerKey);
        consCert.Allocated(allocationId, prodCert, prodParams, consParams, ownerKey);

        var request = FakeRegister.CreateProductionClaim(allocationId, prodCert, consCert, ownerKey, exists: false);
        var result = await Verifier.Verify(request);

        AssertInvalid(result, "Certificate does not exist");
    }

    [Fact]
    public async Task ProductionClaimedVerifier_Invalid_AllocationNotExist()
    {
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var (consCert, consParams) = FakeRegister.ConsumptionIssued(ownerKey.PublicKey, 250);
        var (prodCert, prodParams) = FakeRegister.ProductionIssued(ownerKey.PublicKey, 250);
        var allocationId = Guid.NewGuid();
        consCert.Allocated(allocationId, prodCert, prodParams, consParams, ownerKey);

        var request = FakeRegister.CreateProductionClaim(allocationId, prodCert, consCert, ownerKey);
        var result = await Verifier.Verify(request);

        AssertInvalid(result, "Allocation does not exist");
    }

    [Fact]
    public async Task ProductionClaimedVerifier_Invalid_InvalidSignature()
    {
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var otherKey = Key.Create(SignatureAlgorithm.Ed25519);
        var (consCert, consParams) = FakeRegister.ConsumptionIssued(ownerKey.PublicKey, 250);
        var (prodCert, prodParams) = FakeRegister.ProductionIssued(ownerKey.PublicKey, 250);
        var allocationId = prodCert.Allocated(consCert, prodParams, consParams, ownerKey);
        consCert.Allocated(allocationId, prodCert, prodParams, consParams, ownerKey);

        var request = FakeRegister.CreateProductionClaim(allocationId, prodCert, consCert, otherKey);
        var result = await Verifier.Verify(request);

        AssertInvalid(result, "Invalid signature for slice");
    }

    [Fact]
    public async Task ProductionClaimedVerifier_Invalid_ConsumptionNotFound()
    {
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var (consCert, consParams) = FakeRegister.ConsumptionIssued(ownerKey.PublicKey, 250);
        var (prodCert, prodParams) = FakeRegister.ProductionIssued(ownerKey.PublicKey, 250);
        var allocationId = prodCert.Allocated(consCert, prodParams, consParams, ownerKey);
        consCert.Allocated(allocationId, prodCert, prodParams, consParams, ownerKey);

        var request = FakeRegister.CreateProductionClaim(allocationId, prodCert, consCert, ownerKey, otherExists: false);
        var result = await Verifier.Verify(request);

        AssertInvalid(result, "ConsumptionCertificate does not exist");
    }

    [Fact]
    public async Task ProductionClaimedVerifier_Invalid_ConsumptionNotAllocated()
    {
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var (consCert, consParams) = FakeRegister.ConsumptionIssued(ownerKey.PublicKey, 250);
        var (prodCert, prodParams) = FakeRegister.ProductionIssued(ownerKey.PublicKey, 250);
        var allocationId = prodCert.Allocated(consCert, prodParams, consParams, ownerKey);

        var request = FakeRegister.CreateProductionClaim(allocationId, prodCert, consCert, ownerKey);
        var result = await Verifier.Verify(request);

        AssertInvalid(result, "Consumption not allocated");
    }
}
