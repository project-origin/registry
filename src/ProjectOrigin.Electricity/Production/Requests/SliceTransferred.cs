using NSec.Cryptography;
using ProjectOrigin.Electricity.Shared.Internal;
using ProjectOrigin.RequestProcessor.Interfaces;
using ProjectOrigin.RequestProcessor.Models;

namespace ProjectOrigin.Electricity.Production.Requests;

internal record ProductionSliceTransferredEvent(
    FederatedStreamId CertificateId,
    Slice Slice,
    byte[] NewOwner);

internal record ProductionSliceTransferredRequest(
    SliceParameters SliceParameters,
    ProductionSliceTransferredEvent Event,
    byte[] Signature
    ) : PublishRequest<ProductionSliceTransferredEvent>(Event.CertificateId, Signature, Event);

internal class ProductionSliceTransferredVerifier : SliceVerifier, IRequestVerifier<ProductionSliceTransferredRequest, ProductionCertificate>
{
    public ProductionSliceTransferredVerifier(IEventSerializer serializer) : base(serializer)
    {
    }

    public Task<VerificationResult> Verify(ProductionSliceTransferredRequest request, ProductionCertificate? model)
    {
        if (model is null)
            return VerificationResult.Invalid("Certificate does not exist");

        if (!PublicKey.TryImport(SignatureAlgorithm.Ed25519, request.Event.NewOwner, KeyBlobFormat.RawPublicKey, out _))
            return VerificationResult.Invalid("Invalid NewOwner key, not a valid Ed25519 publicKey");

        var sliceFound = model.GetSlice(request.Event.Slice.Source);
        var verificationResult = VerifySlice(request, request.SliceParameters, request.Event.Slice, sliceFound);
        if (!verificationResult.IsValid)
            return verificationResult;

        return VerificationResult.Valid;
    }
}
