using ProjectOrigin.Register.LineProcessor.Models;

namespace ProjectOrigin.Register.LineProcessor.Interfaces;

public interface IModelLoader
{
    Task<(T? model, int eventCount)> Get<T>(FederatedStreamId eventStreamId) where T : class, IModel;
    Task<(IModel? model, int eventCount)> Get(FederatedStreamId eventStreamId, Type type);
}
