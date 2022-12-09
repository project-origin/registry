using System.Diagnostics.CodeAnalysis;
using ProjectOrigin.Register.StepProcessor.Models;
using ProjectOrigin.Register.V1;

namespace ProjectOrigin.Register.StepProcessor.Interfaces;

public interface IEventVerifier<TModel, TEvent>
{
    Task<VerificationResult> Verify(VerificationRequest<TEvent> request);
}

public interface IEventVerifier<TEvent>
{
    Task<VerificationResult> Verify(VerificationRequest<TEvent> request);
}


public record VerificationRequest<TEvent>
{
    private Dictionary<FederatedStreamId, object> _models;

    public TEvent Event { get; init; }
    public byte[] Signature { get; init; }

    public VerificationRequest(TEvent @event, byte[] signature, Dictionary<Register.V1.FederatedStreamId, object> Models)
    {
        Event = @event;
        Signature = signature;
        _models = Models ?? new Dictionary<FederatedStreamId, object>();
    }

    public bool TryGetModel<T>(FederatedStreamId fid, [MaybeNullWhen(false)] out T model) where T : class
    {
        if (_models.TryGetValue(fid, out var foundModel)
            && foundModel is T)
        {
            model = (T)foundModel;
            return true;
        }
        else
        {
            model = null;
            return false;
        }
    }
}
