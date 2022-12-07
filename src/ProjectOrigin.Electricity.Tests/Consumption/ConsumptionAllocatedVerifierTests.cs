// using NSec.Cryptography;
// using ProjectOrigin.Electricity.Consumption.Requests;
// using ProjectOrigin.Electricity.Production;
// using ProjectOrigin.Register.StepProcessor.Interfaces;
// using ProjectOrigin.Register.StepProcessor.Models;

// namespace ProjectOrigin.Electricity.Tests;

// public class ConsumptionAllocatedVerifierTests
// {
//     private ConsumptionAllocatedVerifier Verifier(ProductionCertificate? pc)
//     {
//         return new ConsumptionAllocatedVerifier();
//     }

//     [Fact]
//     public async Task Verifier_AllocateCertificate_Valid()
//     {
//         var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);

//         var (prodCert, prodParams) = FakeRegister.ProductionIssued(ownerKey.PublicKey, 250);
//         var (consCert, consParams) = FakeRegister.ConsumptionIssued(ownerKey.PublicKey, 300);
//         var quantity = FakeRegister.Group.Commit(150);
//         var (allocationId, _) = prodCert.Allocated(prodParams, consCert.Id, quantity);
//         var verifier = Verifier(prodCert);

//         var request = FakeRegister.CreateConsumptionAllocationRequest(allocationId, consCert.Id, prodCert.Id, consParams, quantity, ownerKey);

//         var result = await verifier.Verify(request, consCert);

//         Assert.IsType<VerificationResult.Valid>(result);
//     }

//     [Fact]
//     public async Task Verifier_AllocateCertificate_CertNotFould()
//     {
//         var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);

//         var (prodCert, prodParams) = FakeRegister.ProductionIssued(ownerKey.PublicKey, 250);
//         var (consCert, consParams) = FakeRegister.ConsumptionIssued(ownerKey.PublicKey, 300);
//         var quantity = FakeRegister.Group.Commit(150);
//         var (allocationId, _) = prodCert.Allocated(prodParams, consCert.Id, quantity);
//         var verifier = Verifier(prodCert);

//         var request = FakeRegister.CreateConsumptionAllocationRequest(allocationId, consCert.Id, prodCert.Id, consParams, quantity, ownerKey);

//         var result = await verifier.Verify(request, null);

//         var invalid = result as VerificationResult.Invalid;
//         Assert.NotNull(invalid);
//         Assert.Equal("Certificate does not exist", invalid!.ErrorMessage);
//     }

//     [Fact]
//     public async Task Verifier_AllocateCertificate_AllocationNotFound()
//     {
//         var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);

//         var (prodCert, prodParams) = FakeRegister.ProductionIssued(ownerKey.PublicKey, 250);
//         var (consCert, consParams) = FakeRegister.ConsumptionIssued(ownerKey.PublicKey, 300);
//         var quantity = FakeRegister.Group.Commit(150);
//         var (allocationId, _) = prodCert.Allocated(prodParams, consCert.Id, quantity);
//         var verifier = Verifier(prodCert);

//         var request = FakeRegister.CreateConsumptionAllocationRequest(Guid.NewGuid(), consCert.Id, prodCert.Id, consParams, quantity, ownerKey);

//         var result = await verifier.Verify(request, consCert);

//         var invalid = result as VerificationResult.Invalid;
//         Assert.NotNull(invalid);
//         Assert.Equal("Production not allocated", invalid!.ErrorMessage);
//     }

//     [Fact]
//     public async Task Verifier_AllocateCertificate_DifferentCommitmentsSameQuantity()
//     {
//         var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);

//         var (prodCert, prodParams) = FakeRegister.ProductionIssued(ownerKey.PublicKey, 250);
//         var (consCert, consParams) = FakeRegister.ConsumptionIssued(ownerKey.PublicKey, 300);
//         var quantity1 = FakeRegister.Group.Commit(150);
//         var quantity2 = FakeRegister.Group.Commit(150);
//         var (allocationId, _) = prodCert.Allocated(prodParams, consCert.Id, quantity1);
//         var verifier = Verifier(prodCert);

//         var request = FakeRegister.CreateConsumptionAllocationRequest(allocationId, consCert.Id, prodCert.Id, consParams, quantity2, ownerKey);

//         var result = await verifier.Verify(request, consCert);

//         var invalid = result as VerificationResult.Invalid;
//         Assert.NotNull(invalid);
//         Assert.Equal("Commmitment are not the same", invalid!.ErrorMessage);
//     }
// }
