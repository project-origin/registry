using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using ProjectOrigin.Registry.V1;
using ProjectOrigin.Registry.Extensions;
using ProjectOrigin.Registry.Repository.Models;

namespace ProjectOrigin.Registry.Repository.InMemory;

public class InMemoryRepository : ITransactionRepository
{
    private readonly object _lockObject = new();
    private readonly List<StreamTransaction> _events = new();
    private readonly List<BlockRecord> _blocks = new();

    public Task<NewBlock?> CreateNextBlock()
    {
        lock (_lockObject)
        {
            var previousBlock = _blocks.OrderByDescending(x => x.ToTransaction).FirstOrDefault();
            if (previousBlock is not null && previousBlock.Publication is null)
            {
                var blockTransactions = _events.Skip(previousBlock.FromTransaction).Take(previousBlock.ToTransaction - previousBlock.FromTransaction + 1).ToList();
                return Task.FromResult<NewBlock?>(new(previousBlock.Header, blockTransactions.Select(x => x.TransactionHash).ToList()));
            }

            var fromTransaction = (previousBlock?.ToTransaction ?? -1) + 1; //-1 since we are 0 indexed
            if (_events.Count <= fromTransaction)
                return Task.FromResult<NewBlock?>(null);

            var numberOfTransactions = (int)BlockSizeCalculator.CalculateBlockLength(_events.Count - fromTransaction);

            var transactions = _events.Skip(fromTransaction).Take(numberOfTransactions).ToList();

            var merkleRootHash = transactions.CalculateMerkleRoot(x => x.Payload);
            var previousHeaderHash = previousBlock is not null ? SHA256.HashData(previousBlock.Header.ToByteArray()) : new byte[32];
            var previousPublicationHash = previousBlock is not null ? SHA256.HashData(previousBlock!.Publication!.ToByteArray()) : new byte[32];

            var blockHeader = new Registry.V1.BlockHeader
            {
                PreviousHeaderHash = ByteString.CopyFrom(previousHeaderHash),
                PreviousPublicationHash = ByteString.CopyFrom(previousPublicationHash),
                MerkleRootHash = ByteString.CopyFrom(merkleRootHash),
                CreatedAt = Timestamp.FromDateTimeOffset(DateTimeOffset.UtcNow)
            };

            var newBlock = new BlockRecord(blockHeader, null, fromTransaction, fromTransaction + numberOfTransactions - 1);
            _blocks.Add(newBlock);

            return Task.FromResult<NewBlock?>(new(blockHeader, transactions.Select(x => x.TransactionHash).ToList()));
        }
    }

    public Task FinalizeBlock(BlockHash hash, Registry.V1.BlockPublication publication)
    {
        lock (_lockObject)
        {
            var foundBlock = _blocks.Find(block => BlockHash.FromHeader(block.Header) == hash && block.Publication is null);
            if (foundBlock is not null)
            {
                _blocks.Remove(foundBlock);
                _blocks.Add(new BlockRecord(foundBlock.Header, publication, foundBlock.FromTransaction, foundBlock.ToTransaction));
                return Task.CompletedTask;
            }
            else
            {
                throw new InvalidOperationException("Block not found or already published or publication does not match");
            }
        }
    }

    public Task<Registry.V1.Block?> GetBlock(TransactionHash transactionHash)
    {
        lock (_lockObject)
        {
            var e = _events.SingleOrDefault(x => x.TransactionHash == transactionHash);

            if (e is null)
                return Task.FromResult<Registry.V1.Block?>(null);

            var index = _events.IndexOf(e);

            var block = _blocks.SingleOrDefault(x => x.FromTransaction <= index && index <= x.ToTransaction);

            if (block is null)
                return Task.FromResult<Registry.V1.Block?>(null);

            var result = new Registry.V1.Block
            {
                Header = block.Header,
                Publication = block.Publication
            };

            return Task.FromResult<Registry.V1.Block?>(result);
        }
    }

    public Task<IList<Block>> GetBlocks(int skip, int take, bool includeTransactions)
    {
        lock (_lockObject)
        {
            var data = _blocks.Skip(skip).Take(take).Select(x =>
            {
                var block = new Block
                {
                    Header = x.Header,
                    Publication = x.Publication,
                    Height = _blocks.IndexOf(x) + 1
                };

                if (includeTransactions)
                {
                    var transactions = _events
                        .Skip(x.FromTransaction)
                        .Take(x.ToTransaction - x.FromTransaction + 1)
                        .Select(y => Transaction.Parser.ParseFrom(y.Payload))
                        .ToList();
                    block.Transactions.AddRange(transactions);
                }

                return block;
            }
            ).ToList();

            return Task.FromResult<IList<Block>>(data);
        }
    }

    public Task<IList<StreamTransaction>> GetStreamTransactionsForBlock(BlockHash blockHash)
    {
        lock (_lockObject)
        {
            var block = _blocks.Single(block => BlockHash.FromHeader(block.Header) == blockHash);

            var events = _events.Skip(block.FromTransaction).Take(block.ToTransaction - block.FromTransaction + 1);

            return Task.FromResult<IList<StreamTransaction>>(events.ToList());
        }
    }

    public Task<IList<StreamTransaction>> GetStreamTransactionsForStream(Guid streamId)
    {
        lock (_lockObject)
        {
            var events = _events.Where(x => x.StreamId == streamId);

            return Task.FromResult<IList<StreamTransaction>>(events.ToList());
        }
    }

    public Task<TransactionStatus> GetTransactionStatus(TransactionHash transactionHash)
    {
        lock (_lockObject)
        {
            var e = _events.SingleOrDefault(x => x.TransactionHash == transactionHash);

            if (e is null)
                return Task.FromResult(TransactionStatus.Unknown);

            var index = _events.IndexOf(e);

            var block = _blocks.SingleOrDefault(x => x.FromTransaction <= index && index <= x.ToTransaction);

            if (block is not null && block.Publication is not null)
                return Task.FromResult(TransactionStatus.Finalized);

            return Task.FromResult(TransactionStatus.Committed);
        }
    }

    public Task Store(StreamTransaction @event)
    {
        lock (_lockObject)
        {
            var nextExpectedIndex = _events.Where(x => x.StreamId == @event.StreamId).Select(x => x.StreamIndex).DefaultIfEmpty(-1).Max() + 1;
            if (nextExpectedIndex != @event.StreamIndex)
                throw new OutOfOrderException($"The transaction on stream {@event.StreamId} has an invalid stream index.");

            _events.Add(@event);
        }

        return Task.CompletedTask;
    }


    private sealed record BlockRecord(Registry.V1.BlockHeader Header, Registry.V1.BlockPublication? Publication, int FromTransaction, int ToTransaction);
}
