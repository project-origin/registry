using NSec.Cryptography;
using ProjectOrigin.Electricity.Models;
using ProjectOrigin.Register.StepProcessor.Interfaces;
using ProjectOrigin.Register.StepProcessor.Models;

namespace ProjectOrigin.Electricity.Production.Requests;

public class ProductionSliceTransferredVerifier : ICommandStepVerifier<V1.TransferProductionSliceCommand.Types.ProductionSliceTransferredEvent, ProductionCertificate>
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

        var certificateSlice = model.GetCertificateSlice(@event.Slice.ToModel());
        if (certificateSlice is null)
            return new VerificationResult.Invalid("Slice not found");

        var verificationResult = certificateSlice.Verify(commandStep.SignedEvent, proof.ToModel(), @event.Slice.ToModel());
        if (verificationResult is VerificationResult.Invalid)
            return verificationResult;

        return new VerificationResult.Valid();
    }
}
