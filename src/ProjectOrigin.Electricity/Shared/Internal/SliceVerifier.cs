using System.Numerics;
using NSec.Cryptography;
using ProjectOrigin.PedersenCommitment;
using ProjectOrigin.RequestProcessor.Interfaces;
using ProjectOrigin.RequestProcessor.Models;

namespace ProjectOrigin.Electricity.Shared.Internal;

internal abstract class SliceVerifier
{
    internal IEventSerializer serializer;

    public SliceVerifier(IEventSerializer serializer)
    {
        this.serializer = serializer;
    }

    public VerificationResult VerifySlice(PublishRequest request, SliceParameters parameters, Slice slice, CertificateSlice? certificateSlice)
    {
        if (parameters.Quantity.m > parameters.Source.m)
            return VerificationResult.Invalid("Transfer larger than source");

        if (parameters.Quantity.m <= 0)
            return VerificationResult.Invalid("Negative or zero transfer not allowed");

        if (!parameters.Source.Verify(slice.Source))
            return VerificationResult.Invalid("Calculated Source commitment does not equal the parameters");

        if (!parameters.Quantity.Verify(slice.Quantity))
            return VerificationResult.Invalid("Calculated Transferred commitment does not equal the parameters");

        if (!parameters.Remainder.Verify(slice.Remainder))
            return VerificationResult.Invalid("Calculated Remainder commitment does not equal the parameters");

        if (certificateSlice is null)
            return VerificationResult.Invalid("Slice not found");

        var data = serializer.Serialize(request.Event);
        if (!Ed25519.Ed25519.Verify(certificateSlice.Owner, data, request.Signature))
            return VerificationResult.Invalid($"Invalid signature");

        var group = parameters.Source.Group;
        var rZero = (parameters.Source.r - (parameters.Quantity.r + parameters.Remainder.r)).MathMod(group.q);
        if (slice.ZeroR != rZero)
            return VerificationResult.Invalid("R to zero is not valid");

        var cZero = Commitment.Create(group, 0, rZero).C;
        if (cZero != (slice.Source / (slice.Quantity * slice.Remainder)).C)
            return VerificationResult.Invalid("R to zero is not valid");

        return VerificationResult.Valid;
    }
}
