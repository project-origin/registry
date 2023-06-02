using Google.Protobuf;
using ProjectOrigin.Electricity.Production;
using ProjectOrigin.Electricity.V1;
using ProjectOrigin.Registry.Utils;
using ProjectOrigin.Registry.Utils.Interfaces;
using ProjectOrigin.Registry.V1;

namespace ProjectOrigin.Electricity.Consumption.Verifiers;

public class ConsumptionAllocatedVerifier : IEventVerifier<ConsumptionCertificate, V1.AllocatedEvent>
{
    private IRemoteModelLoader _remoteModelLoader;

    public ConsumptionAllocatedVerifier(IRemoteModelLoader something)
    {
        _remoteModelLoader = something;
    }

    public async Task<VerificationResult> Verify(Transaction transaction, ConsumptionCertificate? consumptionCertificate, AllocatedEvent payload)
    {
        if (consumptionCertificate is null)
            return new VerificationResult.Invalid("Certificate does not exist");

        var consumptionSlice = consumptionCertificate.GetCertificateSlice(payload.ConsumptionSourceSlice);
        if (consumptionSlice is null)
            return new VerificationResult.Invalid("Slice not found");

        if (consumptionSlice.Owner.VerifySignature(transaction.Header.ToByteArray(), transaction.HeaderSignature))
            return new VerificationResult.Invalid($"Invalid signature for slice");

        var productionCertificate = await _remoteModelLoader.GetModel<ProductionCertificate>(payload.ProductionCertificateId);
        if (productionCertificate is null)
            return new VerificationResult.Invalid("ProductionCertificate does not exist");

        if (!productionCertificate.HasAllocation(payload.AllocationId))
            return new VerificationResult.Invalid("Production not allocated");

        return new VerificationResult.Valid();
    }

}
