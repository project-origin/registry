using System.Numerics;
using Google.Protobuf;
using NSec.Cryptography;
using ProjectOrigin.PedersenCommitment;
using ProjectOrigin.Register.LineProcessor.Models;

namespace ProjectOrigin.Electricity.Shared.Internal;

internal record AllocationSlice(Commitment Commitment, PublicKey Owner, Guid AllocationId, FederatedStreamId ProductionCertificateId, FederatedStreamId ConsumptionCertificateId) : CertificateSlice(Commitment, Owner);

internal record CertificateSlice(Commitment Commitment, PublicKey Owner)
{

    public VerificationResult Verify(SignedEvent signedEvent, SliceProof proof, Slice slice)
    {
        if (proof.Quantity.m > proof.Source.m)
            return new VerificationResult.Invalid("Transfer larger than source");

        if (proof.Quantity.m <= 0)
            return new VerificationResult.Invalid("Negative or zero transfer not allowed");

        if (!proof.Source.Verify(slice.Source))
            return new VerificationResult.Invalid("Calculated Source commitment does not equal the parameters");

        if (!proof.Quantity.Verify(slice.Quantity))
            return new VerificationResult.Invalid("Calculated Transferred commitment does not equal the parameters");

        if (!proof.Remainder.Verify(slice.Remainder))
            return new VerificationResult.Invalid("Calculated Remainder commitment does not equal the parameters");

        if (!signedEvent.VerifySignature(this.Owner))
            return new VerificationResult.Invalid($"Invalid signature");

        var rZero = (proof.Source.r - (proof.Quantity.r + proof.Remainder.r)).MathMod(Group.Default.q);
        if (slice.ZeroR != rZero)
            return new VerificationResult.Invalid("R to zero is not valid");

        var calculatedCommitmentToZero = Commitment.Create(Group.Default, 0, rZero).C;
        if (calculatedCommitmentToZero != (slice.Source / (slice.Quantity * slice.Remainder)).C)
            return new VerificationResult.Invalid("R to zero is not valid");

        return new VerificationResult.Valid();
    }
}
