using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using ProjectOrigin.RequestProcessor.Interfaces;
using ProjectOrigin.RequestProcessor.Models;
using ProjectOrigin.VerifiableEventStore.Models;
using ProjectOrigin.VerifiableEventStore.Services.Batcher;

namespace ProjectOrigin.RequestProcessor.Services;

public class SynchronousInMemoryRequestProcessor : IRequestProcessor
{
    private ConcurrentDictionary<RequestId, RequestResult> requestDictionary = new();
    private RequestProcessorOptions options;
    private IDispatcher dispatcher;
    private IBatcher batcher;
    private IEventSerializer eventSerializer;

    public SynchronousInMemoryRequestProcessor(IOptions<RequestProcessorOptions> options, IDispatcher dispatcher, IBatcher batcher, IEventSerializer eventSerializer)
    {
        this.options = options.Value;
        this.dispatcher = dispatcher;
        this.batcher = batcher;
        this.eventSerializer = eventSerializer;
    }

    public async Task QueueRequest(IPublishRequest request)
    {
        if (request.FederatedStreamId.Registry != options.RegistryName)
            throw new InvalidDataException($"Invalid registry for request");

        if (requestDictionary.TryAdd(request.RequestId, new RequestResult(request.RequestId, RequestState.Queued)))
        {
            await ProcessRequest(request);
        }
        else
        {
            throw new ArgumentException($"Request already exists");
        }
    }

    public Task<RequestResult> GetRequestStatus(RequestId id)
    {
        if (requestDictionary.TryGetValue(id, out var requestStatus))
        {
            return Task.FromResult(requestStatus);
        }
        else
        {
            return Task.FromResult(new RequestResult(id, RequestState.Unknown));
        }
    }

    private async Task ProcessRequest(IPublishRequest request)
    {
        var (result, nextEventIndex) = await dispatcher.Verify(request);

        if (result.IsValid)
        {
            SetState(request.RequestId, RequestState.Processing);
            var serializedEvent = eventSerializer.Serialize(new EventId(request.FederatedStreamId.StreamId, nextEventIndex), request.Event);

            await batcher.PublishEvent(serializedEvent);
            SetState(request.RequestId, RequestState.Completed);
        }
        else
        {
            if (result.ErrorMessage is null)
                throw new InvalidOperationException("Verification failed without errorMessage!");

            SetState(request.RequestId, RequestState.Failed, result.ErrorMessage);
        }
    }

    private void SetState(RequestId id, RequestState state, string? exception = null)
    {
        if (requestDictionary.TryGetValue(id, out var oldState))
        {
            requestDictionary.TryUpdate(id, new RequestResult(id, state, exception), oldState);
        }
    }
}
