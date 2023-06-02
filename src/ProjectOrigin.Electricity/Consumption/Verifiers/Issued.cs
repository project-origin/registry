using Google.Protobuf;
using Microsoft.Extensions.Options;
using ProjectOrigin.Electricity.Extensions;
using ProjectOrigin.Electricity.Interfaces;
using ProjectOrigin.Electricity.Models;
using ProjectOrigin.Registry.Utils;
using ProjectOrigin.Registry.V1;

namespace ProjectOrigin.Electricity.Consumption.Verifiers;

public class ConsumptionIssuedVerifier : IEventVerifier<V1.ConsumptionIssuedEvent>
{
    private IssuerOptions _issuerOptions;
    private IKeyAlgorithm _keyAlgorithm;

    public ConsumptionIssuedVerifier(IOptions<IssuerOptions> issuerOptions, IKeyAlgorithm keyAlgorithm)
    {
        _issuerOptions = issuerOptions.Value;
        _keyAlgorithm = keyAlgorithm;
    }

    public Task<VerificationResult> Verify(Transaction transaction, object? model, V1.ConsumptionIssuedEvent tEvent)
    {
        if (model is not null)
            return new VerificationResult.Invalid($"Certificate with id ”{tEvent.CertificateId.StreamId}” already exists");

        if (!tEvent.QuantityCommitment.VerifyCommitment(tEvent.CertificateId.StreamId.Value))
            return new VerificationResult.Invalid("Invalid range proof for Quantity commitment");

        if (!_keyAlgorithm.TryImport(tEvent.OwnerPublicKey.Content.Span, out _))
            return new VerificationResult.Invalid("Invalid owner key, not a valid publicKey");

        var publicKey = _issuerOptions.GetAreaPublicKey(tEvent.GridArea);
        if (publicKey is null)
            return new VerificationResult.Invalid($"No issuer found for GridArea ”{tEvent.GridArea}”");

        if (!publicKey.VerifySignature(transaction.Header.ToByteArray(), transaction.HeaderSignature))
            return new VerificationResult.Invalid($"Invalid issuer signature for GridArea ”{tEvent.GridArea}”");

        return new VerificationResult.Valid();
    }
}
