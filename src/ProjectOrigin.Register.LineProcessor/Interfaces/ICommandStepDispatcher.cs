using ProjectOrigin.Register.LineProcessor.Models;

namespace ProjectOrigin.Register.LineProcessor.Interfaces;

public interface ICommandStepDispatcher
{
    Task<(VerificationResult Result, int NextEventIndex)> Verify(CommandStep request);
}
