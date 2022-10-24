using ProjectOrigin.RequestProcessor.Models;

namespace ProjectOrigin.RequestProcessor.Interfaces;

public interface IRequestProcessor
{
    Task QueueRequest(IPublishRequest request);
    Task<RequestResult> GetRequestStatus(RequestId id);
}
