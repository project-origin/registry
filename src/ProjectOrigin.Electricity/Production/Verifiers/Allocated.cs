using ProjectOrigin.Electricity.Consumption;
using ProjectOrigin.Electricity.Extensions;
using ProjectOrigin.PedersenCommitment;
using ProjectOrigin.Verifier.Utils;
using ProjectOrigin.Verifier.Utils.Interfaces;
using ProjectOrigin.Registry.V1;
using System.Threading.Tasks;

namespace ProjectOrigin.Electricity.Production.Verifiers;

public class ProductionAllocatedVerifier : IEventVerifier<ProductionCertificate, V1.AllocatedEvent>
{
    private IRemoteModelLoader _remoteModelLoader;

    public ProductionAllocatedVerifier(IRemoteModelLoader remoteModelLoader)
    {
        _remoteModelLoader = remoteModelLoader;
    }

    public async Task<VerificationResult> Verify(Transaction transaction, ProductionCertificate? productionCertificate, V1.AllocatedEvent payload)
    {
        if (productionCertificate is null)
            return new VerificationResult.Invalid("Certificate does not exist");

        var productionSlice = productionCertificate.GetCertificateSlice(payload.ProductionSourceSliceHash);
        if (productionSlice is null)
            return new VerificationResult.Invalid("Production slice does not exist");

        if (!transaction.IsSignatureValid(productionSlice.Owner))
            return new VerificationResult.Invalid($"Invalid signature for slice");

        var consumptionCertificate = await _remoteModelLoader.GetModel<ConsumptionCertificate>(payload.ConsumptionCertificateId);
        if (consumptionCertificate is null)
            return new VerificationResult.Invalid("ConsumptionCertificate does not exist");

        if (consumptionCertificate.Period != productionCertificate.Period)
            return new VerificationResult.Invalid("Certificates are not in the same period");

        if (consumptionCertificate.GridArea != productionCertificate.GridArea)
            return new VerificationResult.Invalid("Certificates are not in the same area");

        var consumptionSlice = consumptionCertificate.GetCertificateSlice(payload.ConsumptionSourceSliceHash);
        if (consumptionSlice is null)
            return new VerificationResult.Invalid("Consumption slice does not exist");

        if (!Commitment.VerifyEqualityProof(
            payload.EqualityProof.ToByteArray(),
            productionSlice.Commitment,
            consumptionSlice.Commitment,
            payload.AllocationId.Value))
            return new VerificationResult.Invalid("Invalid Equality proof");

        return new VerificationResult.Valid();
    }
}
