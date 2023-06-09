using Google.Protobuf;
using ProjectOrigin.Electricity.Extensions;
using ProjectOrigin.Electricity.Interfaces;
using ProjectOrigin.Registry.Utils;
using ProjectOrigin.Registry.V1;

namespace ProjectOrigin.Electricity.Production.Verifiers;

public class ProductionTransferredVerifier : IEventVerifier<ProductionCertificate, V1.TransferredEvent>
{
    private IKeyAlgorithm _keyAlgorithm;

    public ProductionTransferredVerifier(IKeyAlgorithm keyAlgorithm)
    {
        _keyAlgorithm = keyAlgorithm;
    }

    public Task<VerificationResult> Verify(Transaction transaction, ProductionCertificate? certificate, V1.TransferredEvent payload)
    {
        if (certificate is null)
            return new VerificationResult.Invalid("Certificate does not exist");

        var certificateSlice = certificate.GetCertificateSlice(payload.SourceSlice);
        if (certificateSlice is null)
            return new VerificationResult.Invalid("Slice not found");

        if (!transaction.IsSignatureValid(certificateSlice.Owner))
            return new VerificationResult.Invalid($"Invalid signature for slice");

        if (!_keyAlgorithm.TryImport(payload.NewOwner.Content.Span, out _))
            return new VerificationResult.Invalid("Invalid NewOwner key, not a valid publicKey");

        return new VerificationResult.Valid();
    }
}
