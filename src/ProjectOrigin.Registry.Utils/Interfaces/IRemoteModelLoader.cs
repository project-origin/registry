using ProjectOrigin.Common.V1;

namespace ProjectOrigin.Registry.Utils.Interfaces;

public interface IRemoteModelLoader
{
    Task<T?> GetModel<T>(FederatedStreamId federatedStreamId) where T : class;
}
