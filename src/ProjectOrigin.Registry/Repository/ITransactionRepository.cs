using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ProjectOrigin.Registry.Repository.Models;

namespace ProjectOrigin.Registry.Repository;

public interface ITransactionRepository
{
    Task Store(StreamTransaction @event);

    Task<Registry.V1.Block?> GetBlock(TransactionHash transactionHash);
    Task<IList<Registry.V1.Block>> GetBlocks(int skip, int take, bool includeTransactions);
    Task<TransactionStatus> GetTransactionStatus(TransactionHash transactionHash);
    Task<IList<StreamTransaction>> GetStreamTransactionsForBlock(BlockHash blockHash);
    Task<IList<StreamTransaction>> GetStreamTransactionsForStream(Guid streamId);

    Task<NewBlock?> CreateNextBlock();
    Task FinalizeBlock(BlockHash hash, Registry.V1.BlockPublication publication);
}
