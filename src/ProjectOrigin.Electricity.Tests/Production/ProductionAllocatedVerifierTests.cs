using NSec.Cryptography;
using ProjectOrigin.Electricity.Models;
using ProjectOrigin.Electricity.Production.Verifiers;

namespace ProjectOrigin.Electricity.Tests;

public class ProductionAllocatedVerifierTests : AbstractVerifierTests
{
    private ProductionAllocatedVerifier Verifier { get => new ProductionAllocatedVerifier(); }

    [Fact]
    public async Task Verifier_AllocateCertificate_Valid()
    {
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var (consCert, consParams) = FakeRegister.ConsumptionIssued(ownerKey.PublicKey, 250);
        var (prodCert, prodParams) = FakeRegister.ProductionIssued(ownerKey.PublicKey, 250);

        var request = FakeRegister.CreateProductionAllocationRequest(prodCert, consCert, prodParams, consParams, ownerKey);
        var result = await Verifier.Verify(request);

        AssertValid(result);
    }

    [Fact]
    public async Task Verifier_AllocateCertificate_ProdCertNotFould()
    {
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var (consCert, consParams) = FakeRegister.ConsumptionIssued(ownerKey.PublicKey, 250);
        var (prodCert, prodParams) = FakeRegister.ProductionIssued(ownerKey.PublicKey, 250);

        var request = FakeRegister.CreateProductionAllocationRequest(prodCert, consCert, prodParams, consParams, ownerKey, exists: false);
        var result = await Verifier.Verify(request);

        AssertInvalid(result, "Certificate does not exist");
    }

    [Fact]
    public async Task Verifier_InvalidProductionSlice_SliceNotFound()
    {
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var (consCert, consParams) = FakeRegister.ConsumptionIssued(ownerKey.PublicKey, 250);
        var (prodCert, prodParams) = FakeRegister.ProductionIssued(ownerKey.PublicKey, 250);

        var request = FakeRegister.CreateProductionAllocationRequest(prodCert, consCert, consParams, consParams, ownerKey);
        var result = await Verifier.Verify(request);

        AssertInvalid(result, "Production slice does not exist");
    }

    [Fact]
    public async Task Verifier_WrongKey_InvalidSignature()
    {
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var otherKey = Key.Create(SignatureAlgorithm.Ed25519);
        var (consCert, consParams) = FakeRegister.ConsumptionIssued(ownerKey.PublicKey, 250);
        var (prodCert, prodParams) = FakeRegister.ProductionIssued(ownerKey.PublicKey, 250);

        var request = FakeRegister.CreateProductionAllocationRequest(prodCert, consCert, prodParams, consParams, otherKey);
        var result = await Verifier.Verify(request);

        AssertInvalid(result, "Invalid signature for slice");
    }

    [Fact]
    public async Task Verifier_AllocateCertificate_ConsCertNotFould()
    {
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var (consCert, consParams) = FakeRegister.ConsumptionIssued(ownerKey.PublicKey, 250);
        var (prodCert, prodParams) = FakeRegister.ProductionIssued(ownerKey.PublicKey, 250);

        var request = FakeRegister.CreateProductionAllocationRequest(prodCert, consCert, prodParams, consParams, ownerKey, otherExists: false);
        var result = await Verifier.Verify(request);

        AssertInvalid(result, "ConsumptionCertificate does not exist");
    }

    [Fact]
    public async Task Verifier_AllocateCertificate_InvalidPeriod()
    {
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var (consCert, consParams) = FakeRegister.ConsumptionIssued(ownerKey.PublicKey, 250);
        var hourLater = new DateInterval(consCert.Period.Start, consCert.Period.End.AddHours(1));
        var (prodCert, prodParams) = FakeRegister.ProductionIssued(ownerKey.PublicKey, 250, periodOverride: hourLater);

        var request = FakeRegister.CreateProductionAllocationRequest(prodCert, consCert, prodParams, consParams, ownerKey);
        var result = await Verifier.Verify(request);

        AssertInvalid(result, "Certificates are not in the same period");
    }

    [Fact]
    public async Task Verifier_AllocateCertificate_InvalidArea()
    {
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var (consCert, consParams) = FakeRegister.ConsumptionIssued(ownerKey.PublicKey, 250, area: "DK1");
        var (prodCert, prodParams) = FakeRegister.ProductionIssued(ownerKey.PublicKey, 250, area: "DK2");

        var request = FakeRegister.CreateProductionAllocationRequest(prodCert, consCert, prodParams, consParams, ownerKey);
        var result = await Verifier.Verify(request);

        AssertInvalid(result, "Certificates are not in the same area");
    }

    [Fact]
    public async Task Verifier_WrongConsumptionSlice_SliceNotFound()
    {
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var (consCert, consParams) = FakeRegister.ConsumptionIssued(ownerKey.PublicKey, 250);
        var (prodCert, prodParams) = FakeRegister.ProductionIssued(ownerKey.PublicKey, 250);

        var request = FakeRegister.CreateProductionAllocationRequest(prodCert, consCert, prodParams, prodParams, ownerKey);
        var result = await Verifier.Verify(request);

        AssertInvalid(result, "Consumption slice does not exist");
    }

    [Fact]
    public async Task Verifier_RandomProofData_InvalidEqualityProof()
    {
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var (consCert, consParams) = FakeRegister.ConsumptionIssued(ownerKey.PublicKey, 250);
        var (prodCert, prodParams) = FakeRegister.ProductionIssued(ownerKey.PublicKey, 250);

        var request = FakeRegister.CreateProductionAllocationRequest(prodCert, consCert, prodParams, consParams, ownerKey, overwrideEqualityProof: new Fixture().Create<byte[]>());
        var result = await Verifier.Verify(request);

        AssertInvalid(result, "Invalid Equality proof");
    }
}
