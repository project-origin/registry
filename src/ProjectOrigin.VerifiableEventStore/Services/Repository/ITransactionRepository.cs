using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ProjectOrigin.VerifiableEventStore.Models;

namespace ProjectOrigin.VerifiableEventStore.Services.Repository;

public interface ITransactionRepository
{
    Task Store(StreamTransaction @event);

    Task<ImmutableLog.V1.Block?> GetBlock(TransactionHash transactionHash);
    Task<TransactionStatus> GetTransactionStatus(TransactionHash transactionHash);
    Task<IList<StreamTransaction>> GetStreamTransactionsForBlock(BlockHash blockHash);
    Task<IList<StreamTransaction>> GetStreamTransactionsForStream(Guid streamId);

    Task<NewBlock?> CreateNextBlock();
    Task FinalizeBlock(BlockHash hash, ImmutableLog.V1.BlockPublication publication);
}
