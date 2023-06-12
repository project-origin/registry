namespace ProjectOrigin.Registry.Utils;

public interface IVerifierDispatcher
{
    Task<VerificationResult> Verify(Registry.V1.Transaction transaction, IEnumerable<Registry.V1.Transaction> stream);
}