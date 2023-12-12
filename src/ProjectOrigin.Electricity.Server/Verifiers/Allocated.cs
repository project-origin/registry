using ProjectOrigin.Electricity.Extensions;
using ProjectOrigin.PedersenCommitment;
using ProjectOrigin.Registry.V1;
using System.Threading.Tasks;
using ProjectOrigin.Electricity.Server.Models;
using System;
using ProjectOrigin.Electricity.Server.Interfaces;

namespace ProjectOrigin.Electricity.Server.Verifiers;

public class AllocatedEventVerifier : IEventVerifier<V1.AllocatedEvent>
{
    private readonly IRemoteModelLoader _remoteModelLoader;

    public AllocatedEventVerifier(IRemoteModelLoader remoteModelLoader)
    {
        _remoteModelLoader = remoteModelLoader;
    }

    public async Task<VerificationResult> Verify(Transaction transaction, GranularCertificate? certificate, V1.AllocatedEvent payload)
    {
        if (certificate is null)
            return new VerificationResult.Invalid("Certificate does not exist");

        GranularCertificate? otherCertificate;

        if (certificate.Type == V1.GranularCertificateType.Production)
        {

            var productionSlice = certificate.GetCertificateSlice(payload.ProductionSourceSliceHash);
            if (productionSlice is null)
                return new VerificationResult.Invalid("Production slice does not exist");

            if (!transaction.IsSignatureValid(productionSlice.Owner))
                return new VerificationResult.Invalid($"Invalid signature for slice");

            otherCertificate = await _remoteModelLoader.GetModel<GranularCertificate>(payload.ConsumptionCertificateId);
            if (otherCertificate is null)
                return new VerificationResult.Invalid("ConsumptionCertificate does not exist");

            if (otherCertificate.Type != V1.GranularCertificateType.Consumption)
                return new VerificationResult.Invalid("ConsumptionCertificate is not a consumption certificate");

            if (!otherCertificate.Period.Equals(certificate.Period))
                return new VerificationResult.Invalid("Certificates are not in the same period");

            if (otherCertificate.GridArea != certificate.GridArea)
                return new VerificationResult.Invalid("Certificates are not in the same area");

            var consumptionSlice = otherCertificate.GetCertificateSlice(payload.ConsumptionSourceSliceHash);
            if (consumptionSlice is null)
                return new VerificationResult.Invalid("Consumption slice does not exist");

            if (!Commitment.VerifyEqualityProof(
                payload.EqualityProof.ToByteArray(),
                productionSlice.Commitment.ToModel(),
                consumptionSlice.Commitment.ToModel(),
                payload.AllocationId.Value))
                return new VerificationResult.Invalid("Invalid Equality proof");
        }
        else if (certificate.Type == V1.GranularCertificateType.Consumption)
        {

            var consumptionSlice = certificate.GetCertificateSlice(payload.ConsumptionSourceSliceHash);
            if (consumptionSlice is null)
                return new VerificationResult.Invalid("Consumption slice does not exist");

            if (!transaction.IsSignatureValid(consumptionSlice.Owner))
                return new VerificationResult.Invalid($"Invalid signature for slice");

            var productionCertificate = await _remoteModelLoader.GetModel<GranularCertificate>(payload.ProductionCertificateId);
            if (productionCertificate is null)
                return new VerificationResult.Invalid("ProductionCertificate does not exist");

            if (!productionCertificate.HasAllocation(payload.AllocationId))
                return new VerificationResult.Invalid("Production not allocated");
        }
        else
            throw new NotSupportedException($"Certificate type ”{certificate.Type.ToString()}” is not supported");

        return new VerificationResult.Valid();
    }
}
