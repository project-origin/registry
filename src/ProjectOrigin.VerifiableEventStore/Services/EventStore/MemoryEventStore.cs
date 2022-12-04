using ProjectOrigin.VerifiableEventStore.Models;
using ProjectOrigin.VerifiableEventStore.Services.Batcher;

namespace ProjectOrigin.VerifiableEventStore.Services.EventStore;

public class MemoryEventStore : IEventStore
{
    private readonly List<Batch> _batches = new List<Batch>();
    private readonly List<BatchWrapper> _batchWrappers = new();
    private readonly BatcherOptions _options;
    private List<VerifiableEvent> _verifiableEvents = new List<VerifiableEvent>();

    public MemoryEventStore(BatcherOptions options)
    {
        _options = options;
    }
    public Task StoreBatch(Batch batch)
    {
        _batches.Add(batch);
        return Task.CompletedTask;
    }

    public Task<Batch?> GetBatch(EventId eventId)
    {
        var b = _batchWrappers.Where(b => b.Batch.Events.Select(e => e.Id).Contains(eventId)).SingleOrDefault();
        if (b is null || b.Batch is null)
        {
            return Task.FromResult<Batch?>(null);
        }
        return Task.FromResult<Batch?>(b.Batch);

        // var batch = _batches.Where(b => b.Events.Select(e => e.Id).Contains(eventId)).SingleOrDefault();
        // return Task.FromResult(batch);
    }

    public Task<IEnumerable<VerifiableEvent>> GetEventsForBatch(Guid batchId)
    {
        var batchWrapper = _batchWrappers.Where(b => b.Guid == batchId).SingleOrDefault();
        if (batchWrapper is null)
        {
            return Task.FromResult(Enumerable.Empty<VerifiableEvent>());
        }
        return Task.FromResult(batchWrapper.Batch.Events.AsEnumerable());
        // return Task.FromResult(_verifiableEvents.AsEnumerable());
    }

    public Task<IEnumerable<VerifiableEvent>> GetEventsForEventStream(Guid streamId)
    {
        var events = _batches.SelectMany(b => b.Events.Where(e => e.Id.EventStreamId == streamId));
        return Task.FromResult(events);
    }

    public Task Store(VerifiableEvent @event)
    {
        _verifiableEvents.Add(@event);

        if (_verifiableEvents.Count >= _options.BatchSizeExponent)
        {
            var batchEvents = _verifiableEvents;
            _verifiableEvents = new List<VerifiableEvent>();
            var batch = new Batch(string.Empty, string.Empty, batchEvents);
            var batchWrapper = new BatchWrapper(Guid.NewGuid(), batch);
            _batchWrappers.Add(batchWrapper);
        }
        return Task.CompletedTask;
    }

    public Task FinalizeBatch(Guid batchId, string blockId, string transactionHash)
    {
        var batch = _batchWrappers.Where(b => b.Guid == batchId).Select(b => b.Batch).FirstOrDefault();
        if (batch is null)
        {
            throw new ArgumentException("Batch not found", nameof(batchId));
        }
        batch = new Batch(blockId, transactionHash, batch.Events);
        _batchWrappers.Remove(_batchWrappers.Single(x => x.Guid == batchId));
        _batchWrappers.Add(new BatchWrapper(batchId, batch));
        return Task.CompletedTask;
    }

    public Task<IEnumerable<Guid>> GetBatchesForFinalization()
    {
        var batches = _batchWrappers.Where(x => x.Batch.BlockId == string.Empty && x.Batch.TransactionId == string.Empty).ToList();
        return Task.FromResult(batches.Select(b => b.Guid));
    }
}

internal class BatchWrapper
{
    public BatchWrapper(Guid guid, Batch batch)
    {
        Guid = guid;
        Batch = batch;
    }

    public Guid Guid { get; }
    public Batch Batch { get; }
}
