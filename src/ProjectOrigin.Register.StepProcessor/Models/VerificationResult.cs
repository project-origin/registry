namespace ProjectOrigin.Register.StepProcessor.Models;

public abstract record VerificationResult()
{
    public sealed record Valid() : VerificationResult;
    public sealed record Invalid(string ErrorMessage) : VerificationResult;

    public static implicit operator Task<VerificationResult>(VerificationResult result) => Task.FromResult(result);
}
