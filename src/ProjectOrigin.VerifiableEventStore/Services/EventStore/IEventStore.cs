using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NBitcoin;
using ProjectOrigin.VerifiableEventStore.Models;

namespace ProjectOrigin.VerifiableEventStore.Services.EventStore;

public interface IEventStore
{
    Task Store(VerifiableEvent @event);

    Task<ImmutableLog.V1.Block?> GetBatchFromTransactionHash(TransactionHash transactionHash);
    Task<IList<VerifiableEvent>> GetEventsForBatch(BatchHash batchHash);

    Task<IList<VerifiableEvent>> GetEventsForEventStream(Guid streamId);
    Task<TransactionStatus> GetTransactionStatus(TransactionHash transactionHash);

    Task<(ImmutableLog.V1.BlockHeader, IList<TransactionHash>)> CreateNextBatch();
    Task FinalizeBatch(BatchHash hash, ImmutableLog.V1.BlockPublication publication);
}
