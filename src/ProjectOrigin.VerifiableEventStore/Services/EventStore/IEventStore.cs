using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ProjectOrigin.VerifiableEventStore.Models;

namespace ProjectOrigin.VerifiableEventStore.Services.EventStore;

public interface IEventStore
{
    Task Store(VerifiableEvent @event);

    Task<Batch?> GetBatch(Guid batchId);
    Task<Batch?> GetBatchFromEventId(EventId eventId);
    Task<Batch?> GetBatchFromTransactionId(string transactionId);

    Task<IEnumerable<VerifiableEvent>> GetEventsForEventStream(Guid streamId);

    Task<IEnumerable<VerifiableEvent>> GetEventsForBatch(Guid batchId);

    Task<bool> TryGetNextBatchForFinalization(out Batch batch);

    Task FinalizeBatch(Guid batchId, string blockId, string transactionHash);

    Task<TransactionStatus> GetTransactionStatus(string transactionId);
}
