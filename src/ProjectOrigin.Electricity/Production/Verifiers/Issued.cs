using Google.Protobuf;
using Microsoft.Extensions.Options;
using ProjectOrigin.Electricity.Extensions;
using ProjectOrigin.Electricity.Interfaces;
using ProjectOrigin.Electricity.Models;
using ProjectOrigin.Registry.Utils;
using ProjectOrigin.Registry.V1;

namespace ProjectOrigin.Electricity.Production.Verifiers;

public class ProductionIssuedVerifier : IEventVerifier<V1.ProductionIssuedEvent>
{
    private IssuerOptions _issuerOptions;
    private IKeyAlgorithm _keyAlgorithm;

    public ProductionIssuedVerifier(IOptions<IssuerOptions> issuerOptions, IKeyAlgorithm keyAlgorithm)
    {
        _issuerOptions = issuerOptions.Value;
        _keyAlgorithm = keyAlgorithm;
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

        if (!_keyAlgorithm.TryImport(payload.OwnerPublicKey.Content.Span, out _))
            return new VerificationResult.Invalid("Invalid owner key, not a valid publicKey");

        var publicKey = _issuerOptions.GetAreaPublicKey(payload.GridArea);
        if (publicKey is null)
            return new VerificationResult.Invalid($"No issuer found for GridArea ”{payload.GridArea}”");

        if (publicKey.VerifySignature(transaction.Header.ToByteArray(), transaction.HeaderSignature))
            return new VerificationResult.Invalid($"Invalid issuer signature for GridArea ”{payload.GridArea}”");

        return new VerificationResult.Valid();
    }
}
