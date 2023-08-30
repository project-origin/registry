using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using ProjectOrigin.VerifiableEventStore.Extensions;
using ProjectOrigin.VerifiableEventStore.Models;
using ProjectOrigin.VerifiableEventStore.Services.Repository;

namespace ProjectOrigin.VerifiableEventStore.Services.EventStore.InMemory;

public class InMemoryRepository : ITransactionRepository
{
    private readonly BlockSizeCalculator _blockSizeCalculator;
    private object lockObject = new();
    private List<StreamTransaction> _events = new();
    private ConcurrentDictionary<BlockHash, BlockRecord> _blocks = new();

    public InMemoryRepository()
    {
        _blockSizeCalculator = new BlockSizeCalculator();
    }

    public Task<ITransactionRepository.NewBlock?> CreateNextBlock()
    {
        lock (lockObject)
        {
            var previousBlock = _blocks.Values.OrderByDescending(x => x.ToTransaction).FirstOrDefault();

            if (previousBlock is not null && previousBlock.Publication is null)
                throw new InvalidOperationException("Previous block has not been published");

            var fromTransaction = (previousBlock?.FromTransaction ?? -1) + 1; //-1 since we are 0 indexed
            var toTransaction = _events.Count;

            if (toTransaction <= fromTransaction)
                return Task.FromResult<ITransactionRepository.NewBlock?>(null);

            toTransaction = fromTransaction + (int)_blockSizeCalculator.CalculateBlockLength(toTransaction - fromTransaction) - 1;

            var transactions = _events.Skip(fromTransaction).Take(toTransaction - fromTransaction + 1).ToList();

            var merkleRootHash = transactions.CalculateMerkleRoot(x => x.Payload);
            var previousHeaderHash = previousBlock is not null ? SHA256.HashData(previousBlock.Header.ToByteArray()) : new byte[32];
            var previousPublicationHash = previousBlock is not null ? SHA256.HashData(previousBlock!.Publication!.ToByteArray()) : new byte[32];

            var blockHeader = new ImmutableLog.V1.BlockHeader
            {
                PreviousHeaderHash = ByteString.CopyFrom(previousHeaderHash),
                PreviousPublicationHash = ByteString.CopyFrom(previousPublicationHash),
                MerkleRootHash = ByteString.CopyFrom(merkleRootHash),
                CreatedAt = Timestamp.FromDateTimeOffset(DateTimeOffset.UtcNow)
            };

            var blockHash = BlockHash.FromHeader(blockHeader);
            _blocks[blockHash] = new BlockRecord(blockHeader, null, fromTransaction, toTransaction);

            IList<TransactionHash> transactionHashes = transactions.Select(x => x.TransactionHash).ToList();

            return Task.FromResult<ITransactionRepository.NewBlock?>(new(blockHeader, transactionHashes));
        }
    }

    public Task FinalizeBlock(BlockHash hash, ImmutableLog.V1.BlockPublication publication)
    {
        lock (lockObject)
        {
            if (_blocks.TryGetValue(hash, out var block) && block.Publication is null)
            {
                _blocks[hash] = new BlockRecord(block.Header, publication, block.FromTransaction, block.ToTransaction);
                return Task.CompletedTask;
            }
            else
            {
                throw new InvalidOperationException("Block not found or already published or publication does not match");
            }
        }
    }

    public Task<ImmutableLog.V1.Block?> GetBlockFromTransactionHash(TransactionHash transactionHash)
    {
        lock (lockObject)
        {
            var e = _events.SingleOrDefault(x => x.TransactionHash == transactionHash);

            if (e is null)
                return Task.FromResult<ImmutableLog.V1.Block?>(null);

            var index = _events.IndexOf(e);

            var block = _blocks.Values.SingleOrDefault(x => x.FromTransaction <= index && index <= x.ToTransaction);

            if (block is null)
                return Task.FromResult<ImmutableLog.V1.Block?>(null);

            var result = new ImmutableLog.V1.Block
            {
                Header = block.Header,
                Publication = block.Publication
            };

            return Task.FromResult<ImmutableLog.V1.Block?>(result);
        }
    }

    public Task<IList<StreamTransaction>> GetStreamTransactionsForBlock(BlockHash blockHash)
    {
        lock (lockObject)
        {
            var block = _blocks[blockHash];

            var events = _events.Skip(block.FromTransaction).Take(block.ToTransaction - block.FromTransaction + 1);

            return Task.FromResult<IList<StreamTransaction>>(events.ToList());
        }
    }

    public Task<IList<StreamTransaction>> GetStreamTransactionsForStream(Guid streamId)
    {
        lock (lockObject)
        {
            var events = _events.Where(x => x.StreamId == streamId);

            return Task.FromResult<IList<StreamTransaction>>(events.ToList());
        }
    }

    public Task<TransactionStatus> GetTransactionStatus(TransactionHash transactionHash)
    {
        lock (lockObject)
        {
            var e = _events.SingleOrDefault(x => x.TransactionHash == transactionHash);

            if (e is null)
                return Task.FromResult(TransactionStatus.Unknown);

            var index = _events.IndexOf(e);

            var block = _blocks.Values.SingleOrDefault(x => x.FromTransaction <= index && index <= x.ToTransaction);

            if (block is not null && block.Publication is not null)
                return Task.FromResult(TransactionStatus.Committed);

            return Task.FromResult(TransactionStatus.Pending);
        }
    }

    public Task Store(StreamTransaction @event)
    {
        lock (lockObject)
        {
            var nextExpectedIndex = _events.Where(x => x.StreamId == @event.StreamId).Select(x => x.StreamIndex).DefaultIfEmpty(-1).Max() + 1;
            if (nextExpectedIndex != @event.StreamIndex)
                throw new OutOfOrderException($"The transaction on stream {@event.StreamId} has an invalid stream index.");

            _events.Add(@event);
        }

        return Task.CompletedTask;
    }

    private record BlockRecord(ImmutableLog.V1.BlockHeader Header, ImmutableLog.V1.BlockPublication? Publication, int FromTransaction, int ToTransaction);
}
