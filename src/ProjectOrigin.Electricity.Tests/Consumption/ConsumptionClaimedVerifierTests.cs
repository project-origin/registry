// using NSec.Cryptography;
// using ProjectOrigin.Electricity.Consumption.Requests;
// using ProjectOrigin.Electricity.Production;
// using ProjectOrigin.Register.StepProcessor.Interfaces;
// using ProjectOrigin.Register.StepProcessor.Models;

// namespace ProjectOrigin.Electricity.Tests;

// public class ConsumptionClaimedVerifierTests
// {
//     private ConsumptionClaimedVerifier Verifier(ProductionCertificate? pc)
//     {
//         return new ConsumptionClaimedVerifier();
//     }

//     [Fact]
//     public async Task Verifier_AllocateCertificate_Valid()
//     {
//         var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);
//         var quantity = FakeRegister.Group.Commit(150);
//         var (prodCert, prodParams) = FakeRegister.ProductionIssued(ownerKey.PublicKey, 250);
//         var (consCert, consParams) = FakeRegister.ConsumptionIssued(ownerKey.PublicKey, 300);
//         var (allocationId, _) = prodCert.Allocated(prodParams, consCert.Id, quantity);
//         consCert.Allocated(allocationId, consParams, prodCert.Id, quantity);
//         prodCert.Claimed(allocationId);

//         var verifier = Verifier(prodCert);
//         var request = FakeRegister.CreateConsumptionClaim(consCert.Id, allocationId, ownerKey);
//         var result = await verifier.Verify(request, consCert);

//         Assert.IsType<VerificationResult.Valid>(result);
//     }

//     [Fact]
//     public async Task Verifier_AllocateCertificate_InvalidSignature()
//     {
//         var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);
//         var otherKey = Key.Create(SignatureAlgorithm.Ed25519);
//         var quantity = FakeRegister.Group.Commit(150);
//         var (prodCert, prodParams) = FakeRegister.ProductionIssued(ownerKey.PublicKey, 250);
//         var (consCert, consParams) = FakeRegister.ConsumptionIssued(ownerKey.PublicKey, 300);
//         var (allocationId, _) = prodCert.Allocated(prodParams, consCert.Id, quantity);
//         consCert.Allocated(allocationId, consParams, prodCert.Id, quantity);
//         prodCert.Claimed(allocationId);

//         var verifier = Verifier(prodCert);
//         var request = FakeRegister.CreateConsumptionClaim(consCert.Id, allocationId, otherKey);
//         var result = await verifier.Verify(request, consCert);

//         var invalid = result as VerificationResult.Invalid;
//         Assert.NotNull(invalid);
//         Assert.Equal("Invalid signature", invalid!.ErrorMessage);
//     }

//     [Fact]
//     public async Task Verifier_AllocateCertificate_NotConsumptionAllocated()
//     {
//         var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);
//         var quantity = FakeRegister.Group.Commit(150);
//         var (prodCert, prodParams) = FakeRegister.ProductionIssued(ownerKey.PublicKey, 250);
//         var (consCert, consParams) = FakeRegister.ConsumptionIssued(ownerKey.PublicKey, 300);
//         var (allocationId, _) = prodCert.Allocated(prodParams, consCert.Id, quantity);
//         prodCert.Claimed(allocationId);

//         var verifier = Verifier(prodCert);
//         var request = FakeRegister.CreateConsumptionClaim(consCert.Id, allocationId, ownerKey);
//         var result = await verifier.Verify(request, consCert);

//         var invalid = result as VerificationResult.Invalid;
//         Assert.NotNull(invalid);
//         Assert.Equal("Allocation does not exist", invalid!.ErrorMessage);
//     }

//     [Fact]
//     public async Task Verifier_AllocateCertificate_CertNotFound()
//     {
//         var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);
//         var quantity = FakeRegister.Group.Commit(150);
//         var (prodCert, prodParams) = FakeRegister.ProductionIssued(ownerKey.PublicKey, 250);
//         var (consCert, consParams) = FakeRegister.ConsumptionIssued(ownerKey.PublicKey, 300);
//         var (allocationId, _) = prodCert.Allocated(prodParams, consCert.Id, quantity);
//         consCert.Allocated(allocationId, consParams, prodCert.Id, quantity);
//         prodCert.Claimed(allocationId);

//         var verifier = Verifier(prodCert);
//         var request = FakeRegister.CreateConsumptionClaim(consCert.Id, allocationId, ownerKey);
//         var result = await verifier.Verify(request, null);

//         var invalid = result as VerificationResult.Invalid;
//         Assert.NotNull(invalid);
//         Assert.Equal("Certificate does not exist", invalid!.ErrorMessage);
//     }

//     [Fact]
//     public async Task Verifier_AllocateCertificate_ProductionNotClaimed()
//     {
//         var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);
//         var quantity = FakeRegister.Group.Commit(150);

//         var (prodCert, prodParams) = FakeRegister.ProductionIssued(ownerKey.PublicKey, 250);
//         var (consCert, consParams) = FakeRegister.ConsumptionIssued(ownerKey.PublicKey, 300);
//         var (allocationId, _) = prodCert.Allocated(prodParams, consCert.Id, quantity);
//         consCert.Allocated(allocationId, consParams, prodCert.Id, quantity);

//         var verifier = Verifier(prodCert);
//         var request = FakeRegister.CreateConsumptionClaim(consCert.Id, allocationId, ownerKey);
//         var result = await verifier.Verify(request, consCert);

//         var invalid = result as VerificationResult.Invalid;
//         Assert.NotNull(invalid);
//         Assert.Equal("Production not claimed", invalid!.ErrorMessage);
//     }
// }
