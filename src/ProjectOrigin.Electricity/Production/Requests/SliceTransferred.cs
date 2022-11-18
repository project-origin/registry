using NSec.Cryptography;
using ProjectOrigin.Electricity.Shared.Internal;
using ProjectOrigin.Register.LineProcessor.Interfaces;
using ProjectOrigin.Register.LineProcessor.Models;

namespace ProjectOrigin.Electricity.Production.Requests;

internal class ProductionSliceTransferredVerifier : ICommandStepVerifier<V1.TransferProductionSliceCommand.Types.ProductionSliceTransferredEvent, ProductionCertificate>
{
    public Task<VerificationResult> Verify(CommandStep<V1.TransferProductionSliceCommand.Types.ProductionSliceTransferredEvent> commandStep, ProductionCertificate? model)
    {
        var @event = commandStep.SignedEvent.Event;

        if (model is null)
            return new VerificationResult.Invalid("Certificate does not exist");

        var proof = commandStep.Proof as V1.SliceProof;
        if (proof is null)
            return new VerificationResult.Invalid("Invalid or missing proof");

        if (!PublicKey.TryImport(SignatureAlgorithm.Ed25519, @event.NewOwner.ToByteArray(), KeyBlobFormat.RawPublicKey, out _))
            return new VerificationResult.Invalid("Invalid NewOwner key, not a valid Ed25519 publicKey");

        var certificateSlice = model.GetCertificateSlice(Slice.From(@event.Slice));
        if (certificateSlice is null)
            return new VerificationResult.Invalid("Slice not found");

        var verificationResult = certificateSlice.Verify(commandStep.SignedEvent, proof, Slice.From(@event.Slice));
        if (verificationResult is VerificationResult.Invalid)
            return verificationResult;

        return new VerificationResult.Valid();
    }
}
