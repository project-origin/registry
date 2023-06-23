using System.Threading.Tasks;

namespace ProjectOrigin.Verifier.Utils;

public interface IEventVerifier<TModel, TEvent> where TEvent : Google.Protobuf.IMessage
{
    public Task<VerificationResult> Verify(Registry.V1.Transaction transaction, TModel? model, TEvent payload);
}

public interface IEventVerifier<TEvent> : IEventVerifier<object, TEvent> where TEvent : Google.Protobuf.IMessage
{
}

public abstract record VerificationResult()
{
    public sealed record Valid() : VerificationResult;
    public sealed record Invalid(string ErrorMessage) : VerificationResult;

    public static implicit operator Task<VerificationResult>(VerificationResult result) => Task.FromResult(result);
}
