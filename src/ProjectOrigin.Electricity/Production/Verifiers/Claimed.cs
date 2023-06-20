using Google.Protobuf;
using ProjectOrigin.Electricity.Consumption;
using ProjectOrigin.Electricity.Extensions;
using ProjectOrigin.Electricity.V1;
using ProjectOrigin.Verifier.Utils;
using ProjectOrigin.Verifier.Utils.Interfaces;
using ProjectOrigin.Registry.V1;
using System.Threading.Tasks;

namespace ProjectOrigin.Electricity.Production.Verifiers;

public class ProductionClaimedVerifier : IEventVerifier<ProductionCertificate, V1.ClaimedEvent>
{
    private IRemoteModelLoader _remoteModelLoader;

    public ProductionClaimedVerifier(IRemoteModelLoader remoteModelLoader)
    {
        _remoteModelLoader = remoteModelLoader;
    }

    public async Task<VerificationResult> Verify(Transaction transaction, ProductionCertificate? productionCertificate, ClaimedEvent payload)
    {
        if (productionCertificate is null)
            return new VerificationResult.Invalid("Certificate does not exist");

        var slice = productionCertificate.GetAllocation(payload.AllocationId);
        if (slice is null)
            return new VerificationResult.Invalid("Allocation does not exist");

        if (!transaction.IsSignatureValid(slice.Owner))
            return new VerificationResult.Invalid($"Invalid signature for slice");

        var consumptionCertificate = await _remoteModelLoader.GetModel<ConsumptionCertificate>(slice.ConsumptionCertificateId);
        if (consumptionCertificate is null)
            return new VerificationResult.Invalid("ConsumptionCertificate does not exist");

        if (!consumptionCertificate.HasAllocation(payload.AllocationId))
            return new VerificationResult.Invalid("Consumption not allocated");

        return new VerificationResult.Valid();
    }
}
