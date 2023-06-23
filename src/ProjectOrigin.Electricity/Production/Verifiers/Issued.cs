using ProjectOrigin.Electricity.Extensions;
using ProjectOrigin.Electricity.Interfaces;
using ProjectOrigin.Verifier.Utils;
using ProjectOrigin.Registry.V1;
using System.Threading.Tasks;

namespace ProjectOrigin.Electricity.Production.Verifiers;

public class ProductionIssuedVerifier : IEventVerifier<V1.ProductionIssuedEvent>
{
    private IGridAreaIssuerService _gridAreaIssuerService;

    public ProductionIssuedVerifier(IGridAreaIssuerService gridAreaIssuerService)
    {
        _gridAreaIssuerService = gridAreaIssuerService;
    }

    public Task<VerificationResult> Verify(Transaction transaction, object? model, V1.ProductionIssuedEvent payload)
    {
        if (model is not null)
            return new VerificationResult.Invalid($"Certificate with id ”{payload.CertificateId.StreamId}” already exists");

        if (!payload.QuantityCommitment.VerifyCommitment(payload.CertificateId.StreamId.Value))
            return new VerificationResult.Invalid("Invalid range proof for Quantity commitment");

        if (payload.QuantityPublication is not null
            && !payload.QuantityCommitment.VerifyPublication(payload.QuantityPublication))
            return new VerificationResult.Invalid($"Private and public quantity proof does not match");

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
