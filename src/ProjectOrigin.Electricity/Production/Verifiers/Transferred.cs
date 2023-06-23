using ProjectOrigin.Electricity.Extensions;
using ProjectOrigin.Verifier.Utils;
using ProjectOrigin.Registry.V1;
using System.Threading.Tasks;

namespace ProjectOrigin.Electricity.Production.Verifiers;

public class ProductionTransferredVerifier : IEventVerifier<ProductionCertificate, V1.TransferredEvent>
{
    public Task<VerificationResult> Verify(Transaction transaction, ProductionCertificate? certificate, V1.TransferredEvent payload)
    {
        if (certificate is null)
            return new VerificationResult.Invalid("Certificate does not exist");

        var certificateSlice = certificate.GetCertificateSlice(payload.SourceSliceHash);
        if (certificateSlice is null)
            return new VerificationResult.Invalid("Slice not found");

        if (!transaction.IsSignatureValid(certificateSlice.Owner))
            return new VerificationResult.Invalid($"Invalid signature for slice");

        if (!payload.NewOwner.TryToModel(out _))
            return new VerificationResult.Invalid("Invalid NewOwner key, not a valid publicKey");

        return new VerificationResult.Valid();
    }
}
