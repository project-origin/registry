namespace ProjectOrigin.RequestProcessor.Interfaces;

public interface IModelLoader
{
    Task<(IModel? model, int eventCount)> Get(Guid eventStreamId, Type type);
}
