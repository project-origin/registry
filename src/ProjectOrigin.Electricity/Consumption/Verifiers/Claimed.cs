using ProjectOrigin.Electricity.Extensions;
using ProjectOrigin.Electricity.Production;
using ProjectOrigin.Electricity.V1;
using ProjectOrigin.Verifier.Utils;
using ProjectOrigin.Verifier.Utils.Interfaces;
using ProjectOrigin.Registry.V1;
using System.Threading.Tasks;

namespace ProjectOrigin.Electricity.Consumption.Verifiers;

public class ConsumptionClaimedVerifier : IEventVerifier<ConsumptionCertificate, V1.ClaimedEvent>
{
    private IRemoteModelLoader _remoteModelLoader;

    public ConsumptionClaimedVerifier(IRemoteModelLoader remoteModelLoader)
    {
        _remoteModelLoader = remoteModelLoader;
    }

    public async Task<VerificationResult> Verify(Transaction transaction, ConsumptionCertificate? consumptionCertificate, ClaimedEvent payload)
    {
        if (consumptionCertificate is null)
            return new VerificationResult.Invalid("Certificate does not exist");

        var slice = consumptionCertificate.GetAllocation(payload.AllocationId);
        if (slice is null)
            return new VerificationResult.Invalid("Allocation does not exist");

        if (!transaction.IsSignatureValid(slice.Owner))
            return new VerificationResult.Invalid($"Invalid signature for slice");

        var productionCertificate = await _remoteModelLoader.GetModel<ProductionCertificate>(slice.ProductionCertificateId);
        if (productionCertificate is null)
            return new VerificationResult.Invalid("ProductionCertificate does not exist");

        if (!productionCertificate.HasClaim(payload.AllocationId))
            return new VerificationResult.Invalid("Production not claimed");

        return new VerificationResult.Valid();
    }
}
