using ProjectOrigin.Register.StepProcessor.Models;

namespace ProjectOrigin.Register.StepProcessor.Interfaces;

public interface ICommandStepDispatcher
{
    Task<(VerificationResult Result, int NextEventIndex)> Verify(CommandStep request);
}
