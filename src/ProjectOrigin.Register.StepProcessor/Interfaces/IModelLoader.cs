using ProjectOrigin.Register.StepProcessor.Models;

namespace ProjectOrigin.Register.StepProcessor.Interfaces;

public interface IModelLoader
{
    Task<(T? model, int eventCount)> Get<T>(FederatedStreamId eventStreamId) where T : class, IModel;
    Task<(IModel? model, int eventCount)> Get(FederatedStreamId eventStreamId, Type type);
}
