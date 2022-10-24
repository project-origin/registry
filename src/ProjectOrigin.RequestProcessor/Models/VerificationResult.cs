namespace ProjectOrigin.RequestProcessor.Models;

public record VerificationResult(bool IsValid, string? ErrorMessage)
{
    public static VerificationResult Valid { get => new VerificationResult(true, null); }
    public static VerificationResult Invalid(string error) => new VerificationResult(false, error);

    public static implicit operator Task<VerificationResult>(VerificationResult result) => Task.FromResult(result);
}
