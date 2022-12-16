using System.Diagnostics.CodeAnalysis;
using ProjectOrigin.Register.V1;

namespace ProjectOrigin.Register.StepProcessor.Models;

internal record VerificationRequest<TEvent>
{
    private IDictionary<FederatedStreamId, object> _models;

    public TEvent Event { get; init; }
    public byte[] Signature { get; init; }

    public VerificationRequest(TEvent @event, byte[] signature, IDictionary<Register.V1.FederatedStreamId, object> Models)
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
