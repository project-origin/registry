using ProjectOrigin.Electricity.Extensions;
using ProjectOrigin.Electricity.Interfaces;
using ProjectOrigin.Verifier.Utils;
using ProjectOrigin.Registry.V1;
using System.Threading.Tasks;

namespace ProjectOrigin.Electricity.Consumption.Verifiers;

public class ConsumptionIssuedVerifier : IEventVerifier<V1.ConsumptionIssuedEvent>
{
    private IGridAreaIssuerService _gridAreaIssuerService;

    public ConsumptionIssuedVerifier(IGridAreaIssuerService gridAreaIssuerService)
    {
        _gridAreaIssuerService = gridAreaIssuerService;
    }

    public Task<VerificationResult> Verify(Transaction transaction, object? model, V1.ConsumptionIssuedEvent tEvent)
    {
        if (model is not null)
            return new VerificationResult.Invalid($"Certificate with id ”{tEvent.CertificateId.StreamId}” already exists");

        if (!tEvent.QuantityCommitment.VerifyCommitment(tEvent.CertificateId.StreamId.Value))
            return new VerificationResult.Invalid("Invalid range proof for Quantity commitment");

        if (!tEvent.OwnerPublicKey.TryToModel(out _))
            return new VerificationResult.Invalid("Invalid owner key, not a valid publicKey");

        var publicKey = _gridAreaIssuerService.GetAreaPublicKey(tEvent.GridArea);
        if (publicKey is null)
            return new VerificationResult.Invalid($"No issuer found for GridArea ”{tEvent.GridArea}”");

        if (!transaction.IsSignatureValid(publicKey))
            return new VerificationResult.Invalid($"Invalid issuer signature for GridArea ”{tEvent.GridArea}”");

        return new VerificationResult.Valid();
    }
}
