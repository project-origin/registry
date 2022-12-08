using NSec.Cryptography;
using ProjectOrigin.Electricity.Consumption.Verifiers;

namespace ProjectOrigin.Electricity.Tests;

public class ConsumptionAllocatedVerifierTests : AbstractVerifierTests
{
    private ConsumptionAllocatedVerifier Verifier { get => new ConsumptionAllocatedVerifier(); }

    [Fact]
    public async Task Verifier_AllocateCertificate_Valid()
    {
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var (consCert, consParams) = FakeRegister.ConsumptionIssued(ownerKey.PublicKey, 250);
        var (prodCert, prodParams) = FakeRegister.ProductionIssued(ownerKey.PublicKey, 250);
        var allocationId = prodCert.Allocated(consCert, prodParams, consParams, ownerKey);

        var request = FakeRegister.CreateConsumptionAllocationRequest(allocationId, prodCert, consCert, prodParams, consParams, ownerKey);
        var result = await Verifier.Verify(request);

        AssertValid(result);
    }

    [Fact]
    public async Task Verifier_CertNotFound_Invalid()
    {
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var (consCert, consParams) = FakeRegister.ConsumptionIssued(ownerKey.PublicKey, 250);
        var (prodCert, prodParams) = FakeRegister.ProductionIssued(ownerKey.PublicKey, 250);
        var allocationId = prodCert.Allocated(consCert, prodParams, consParams, ownerKey);

        var request = FakeRegister.CreateConsumptionAllocationRequest(allocationId, prodCert, consCert, prodParams, consParams, ownerKey, exists: false);
        var result = await Verifier.Verify(request);

        AssertInvalid(result, "Certificate does not exist");
    }

    [Fact]
    public async Task Verifier_SliceNotFound_Invalid()
    {
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var (consCert, consParams) = FakeRegister.ConsumptionIssued(ownerKey.PublicKey, 250);
        var (prodCert, prodParams) = FakeRegister.ProductionIssued(ownerKey.PublicKey, 250);
        var allocationId = prodCert.Allocated(consCert, prodParams, consParams, ownerKey);

        var request = FakeRegister.CreateConsumptionAllocationRequest(allocationId, prodCert, consCert, prodParams, prodParams, ownerKey);
        var result = await Verifier.Verify(request);

        AssertInvalid(result, "Slice not found");
    }

    [Fact]
    public async Task Verifier_InvalidSignatureForSlice_Invalid()
    {
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var otherKey = Key.Create(SignatureAlgorithm.Ed25519);
        var (consCert, consParams) = FakeRegister.ConsumptionIssued(ownerKey.PublicKey, 250);
        var (prodCert, prodParams) = FakeRegister.ProductionIssued(ownerKey.PublicKey, 250);
        var allocationId = prodCert.Allocated(consCert, prodParams, consParams, ownerKey);

        var request = FakeRegister.CreateConsumptionAllocationRequest(allocationId, prodCert, consCert, prodParams, consParams, otherKey);
        var result = await Verifier.Verify(request);

        AssertInvalid(result, "Invalid signature for slice");
    }

    [Fact]
    public async Task Verifier_ProductionNotFound_Invalid()
    {
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var (consCert, consParams) = FakeRegister.ConsumptionIssued(ownerKey.PublicKey, 250);
        var (prodCert, prodParams) = FakeRegister.ProductionIssued(ownerKey.PublicKey, 250);
        var allocationId = prodCert.Allocated(consCert, prodParams, consParams, ownerKey);

        var request = FakeRegister.CreateConsumptionAllocationRequest(allocationId, prodCert, consCert, prodParams, consParams, ownerKey, otherExists: false);
        var result = await Verifier.Verify(request);

        AssertInvalid(result, "ProductionCertificate does not exist");
    }

    [Fact]
    public async Task Verifier_ProdNotAllocated_Invalid()
    {
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var (consCert, consParams) = FakeRegister.ConsumptionIssued(ownerKey.PublicKey, 250);
        var (prodCert, prodParams) = FakeRegister.ProductionIssued(ownerKey.PublicKey, 250);

        var request = FakeRegister.CreateConsumptionAllocationRequest(Guid.NewGuid(), prodCert, consCert, prodParams, consParams, ownerKey);
        var result = await Verifier.Verify(request);

        AssertInvalid(result, "Production not allocated");
    }
}
