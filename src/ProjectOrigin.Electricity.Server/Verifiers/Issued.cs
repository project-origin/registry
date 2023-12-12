using ProjectOrigin.Electricity.Extensions;
using ProjectOrigin.Registry.V1;
using System.Threading.Tasks;
using ProjectOrigin.Electricity.Models;
using ProjectOrigin.Electricity.Server.Interfaces;

namespace ProjectOrigin.Electricity.Server.Verifiers;

public class IssuedEventVerifier : IEventVerifier<V1.IssuedEvent>
{
    private readonly IGridAreaIssuerService _gridAreaIssuerService;

    public IssuedEventVerifier(IGridAreaIssuerService gridAreaIssuerService)
    {
        _gridAreaIssuerService = gridAreaIssuerService;
    }

    public Task<VerificationResult> Verify(Transaction transaction, GranularCertificate? certificate, V1.IssuedEvent payload)
    {
        if (certificate is not null)
            return new VerificationResult.Invalid($"Certificate with id ”{payload.CertificateId.StreamId}” already exists");

        if (!payload.QuantityCommitment.VerifyCommitment(payload.CertificateId.StreamId.Value))
            return new VerificationResult.Invalid("Invalid range proof for Quantity commitment");

        if (payload.Type == V1.GranularCertificateType.Invalid)
            return new VerificationResult.Invalid("Invalid certificate type");

        if (!payload.OwnerPublicKey.TryToModel(out _))
            return new VerificationResult.Invalid("Invalid owner key, not a valid publicKey");

        var areaPublicKey = _gridAreaIssuerService.GetAreaPublicKey(payload.GridArea);
        if (areaPublicKey is null)
            return new VerificationResult.Invalid($"No issuer found for GridArea ”{payload.GridArea}”");

        if (!transaction.IsSignatureValid(areaPublicKey))
            return new VerificationResult.Invalid($"Invalid issuer signature for GridArea ”{payload.GridArea}”");

        return new VerificationResult.Valid();
    }
}
