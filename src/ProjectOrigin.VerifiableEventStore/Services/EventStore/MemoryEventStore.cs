using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using ProjectOrigin.VerifiableEventStore.Models;

namespace ProjectOrigin.VerifiableEventStore.Services.EventStore;

public class MemoryEventStore : IEventStore
{
    private readonly List<BatchWrapper> _batchWrappers = new();
    private readonly int _batchSize;

    private BatchWrapper _currentBatch;
    private List<VerifiableEvent> _currentBatchEvents;

    public MemoryEventStore(IOptions<VerifiableEventStoreOptions> options)
    {
        _batchSize = 1 << options.Value.BatchSizeExponent;

        _currentBatchEvents = new List<VerifiableEvent>();
        _currentBatch = new BatchWrapper(new Batch(Guid.NewGuid(), Guid.Empty, "", ""), _currentBatchEvents);
        _batchWrappers.Add(_currentBatch);
    }

    public async Task Store(VerifiableEvent @event)
    {
        int nextValidIndex = await GetNextValidIndex(@event.Id.EventStreamId);
        if (@event.Id.Index != nextValidIndex)
            throw new OutOfOrderException($"expected {nextValidIndex} got {@event.Id.Index}");

        _currentBatchEvents.Add(@event);

        if (_currentBatchEvents.Count >= _batchSize)
        {
            NewBatch();
        }
    }

    public Task<Batch?> GetBatch(Guid batchId)
    {
        var batchWrapper = _batchWrappers.SingleOrDefault(b => b.Batch.Id == batchId);
        return Task.FromResult<Batch?>(batchWrapper?.Batch);
    }

    public Task<Batch?> GetBatchFromEventId(EventId eventId)
    {
        var batchWrapper = _batchWrappers.SingleOrDefault(b => b.Events.Select(e => e.Id).Contains(eventId));
        return Task.FromResult<Batch?>(batchWrapper?.Batch);
    }

    public Task<Batch?> GetBatchFromTransactionId(string transactionId)
    {
        var batchWrapper = _batchWrappers.SingleOrDefault(x => x.Events.Any(t => t.TransactionId == transactionId));

        return Task.FromResult<Batch?>(batchWrapper?.Batch);
    }

    public Task<IEnumerable<VerifiableEvent>> GetEventsForBatch(Guid batchId)
    {
        var batchWrapper = _batchWrappers.SingleOrDefault(b => b.Batch.Id == batchId);
        return Task.FromResult(batchWrapper?.Events.AsEnumerable()
            ?? Enumerable.Empty<VerifiableEvent>());
    }

    public Task<IEnumerable<VerifiableEvent>> GetEventsForEventStream(Guid streamId)
    {
        var events = _batchWrappers.SelectMany(b => b.Events.Where(e => e.Id.EventStreamId == streamId));
        return Task.FromResult(events.OrderBy(x => x.Id.Index).AsEnumerable());
    }

    public Task FinalizeBatch(Guid batchId, string externalBlockId, string externalTransactionId)
    {

        var foundBatchWrapper = _batchWrappers.FirstOrDefault(b => b.Batch.Id == batchId);
        if (foundBatchWrapper is null)
        {
            throw new ArgumentException("Batch not found", nameof(batchId));
        }
        _batchWrappers.Remove(foundBatchWrapper);
        _batchWrappers.Add(new BatchWrapper(new Batch(foundBatchWrapper.Batch.Id, foundBatchWrapper.Batch.PreviousBatchId, externalBlockId, externalTransactionId), foundBatchWrapper.Events));
        return Task.CompletedTask;
    }

    public Task<TransactionStatus> GetTransactionStatus(string transactionId)
    {
        var batchWrapper = _batchWrappers.SingleOrDefault(x => x.Events.Any(t => t.TransactionId == transactionId));
        if (batchWrapper is null)
            return Task.FromResult(TransactionStatus.Unknown);

        return Task.FromResult(batchWrapper.Batch.IsFinalized ? TransactionStatus.Committed : TransactionStatus.Pending);
    }

    public Task<bool> TryGetNextBatchForFinalization(out Batch batch)
    {
        batch = _batchWrappers.FirstOrDefault(x => !x.Batch.IsFinalized && x.Events.Count == _batchSize)?.Batch!;
        return Task.FromResult(batch is not null);
    }

    private void NewBatch()
    {
        _currentBatchEvents = new List<VerifiableEvent>();
        _currentBatch = new BatchWrapper(new Batch(Guid.NewGuid(), _currentBatch.Batch.Id, "", ""), _currentBatchEvents);
        _batchWrappers.Add(_currentBatch);
    }

    private async Task<int> GetNextValidIndex(Guid streamID)
    {
        var stream = await GetEventsForEventStream(streamID);
        return stream.Select(x => x.Id.Index).DefaultIfEmpty(-1).Max() + 1;
    }

    private record BatchWrapper(Batch Batch, IList<VerifiableEvent> Events);
}
