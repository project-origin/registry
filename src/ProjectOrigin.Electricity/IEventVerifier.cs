using ProjectOrigin.Register.StepProcessor.Models;
using ProjectOrigin.Register.V1;

namespace ProjectOrigin.Electricity.Interfaces;

public interface IEventVerifier<TModel, TEvent>
{
    Task<VerificationResult> Verify(VerificationRequest<TModel, TEvent> request);
}

public record VerificationRequest<TModel, TEvent>
{
    private Dictionary<FederatedStreamId, object> _additionalModels;

    public TModel? Model { get; init; }
    public TEvent Event { get; init; }
    public byte[] Signature { get; init; }

    public VerificationRequest(TModel? model, TEvent @event, byte[] signature, Dictionary<Register.V1.FederatedStreamId, object>? AdditionalModels = null)
    {
        Model = model;
        Event = @event;
        Signature = signature;
        _additionalModels = AdditionalModels ?? new Dictionary<FederatedStreamId, object>();
    }

    public T? GetModel<T>(FederatedStreamId fid) where T : class
    {
        if (_additionalModels.TryGetValue(fid, out var foundModel)
            && foundModel is T)
        {
            return (T)foundModel;
        }
        else
        {
            return null;
        }
    }
}
