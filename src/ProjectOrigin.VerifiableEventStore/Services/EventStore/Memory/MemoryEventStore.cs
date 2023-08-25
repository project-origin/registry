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

namespace ProjectOrigin.VerifiableEventStore.Services.EventStore.Memory;

public class MemoryEventStore : IEventStore
{
    private readonly BlockSizeCalculator _blockSizeCalculator;
    private object lockObject = new();
    private List<VerifiableEvent> _events = new();
    private ConcurrentDictionary<BatchHash, BlockRecord> _blocks = new();

    public MemoryEventStore(BlockSizeCalculator blockSizeCalculator)
    {
        _blockSizeCalculator = blockSizeCalculator;
    }

    public Task<(ImmutableLog.V1.BlockHeader, IList<TransactionHash>)> CreateNextBatch()
    {
        lock (lockObject)
        {
            var previousBlock = _blocks.Values.OrderByDescending(x => x.ToTransaction).FirstOrDefault();

            if (previousBlock is not null && previousBlock.Publication is null)
                throw new InvalidOperationException("Previous block has not been published");

            var fromTransaction = (previousBlock?.FromTransaction ?? -1) + 1; //-1 since we are 0 indexed
            var toTransaction = _events.Count;

            if (toTransaction <= fromTransaction)
                throw new InvalidOperationException("No transactions to batch");

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

            var blockHash = BatchHash.FromHeader(blockHeader);
            _blocks[blockHash] = new BlockRecord(blockHeader, null, fromTransaction, toTransaction);

            IList<TransactionHash> transactionHashes = transactions.Select(x => x.TransactionHash).ToList();

            return Task.FromResult((blockHeader, transactionHashes));
        }
    }

    public Task FinalizeBatch(BatchHash hash, ImmutableLog.V1.BlockPublication publication)
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

    public Task<ImmutableLog.V1.Block?> GetBatchFromTransactionHash(TransactionHash transactionHash)
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

    public Task<IList<VerifiableEvent>> GetEventsForBatch(BatchHash batchHash)
    {
        lock (lockObject)
        {
            var block = _blocks[batchHash];

            var events = _events.Skip(block.FromTransaction).Take(block.ToTransaction - block.FromTransaction + 1);

            return Task.FromResult<IList<VerifiableEvent>>(events.ToList());
        }
    }

    public Task<IList<VerifiableEvent>> GetEventsForEventStream(Guid streamId)
    {
        lock (lockObject)
        {
            var events = _events.Where(x => x.StreamId == streamId);

            return Task.FromResult<IList<VerifiableEvent>>(events.ToList());
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

    public Task Store(VerifiableEvent @event)
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
