using System.Threading.Tasks;
using ProjectOrigin.Electricity.Server.Models;

namespace ProjectOrigin.Electricity.Server.Interfaces;

public interface IEventVerifier<TEvent> where TEvent : Google.Protobuf.IMessage
{
    public Task<VerificationResult> Verify(Registry.V1.Transaction transaction, GranularCertificate? certificate, TEvent payload);
}


public abstract record VerificationResult()
{
    public sealed record Valid() : VerificationResult;
    public sealed record Invalid(string ErrorMessage) : VerificationResult;

    public static implicit operator Task<VerificationResult>(VerificationResult result) => Task.FromResult(result);
}
