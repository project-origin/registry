using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Dapper;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Options;
using Npgsql;
using ProjectOrigin.Registry.V1;
using ProjectOrigin.VerifiableEventStore.Extensions;
using ProjectOrigin.VerifiableEventStore.Models;
using ProjectOrigin.VerifiableEventStore.Services.EventStore.Postgres.Mapping;
using ProjectOrigin.VerifiableEventStore.Services.Repository;

namespace ProjectOrigin.VerifiableEventStore.Services.EventStore.Postgres;

public sealed class PostgresqlRepository : ITransactionRepository, IDisposable
{
    private readonly NpgsqlDataSource _dataSource;

    public PostgresqlRepository(IOptions<PostgresqlEventStoreOptions> options)
    {
        _dataSource = NpgsqlDataSource.Create(options.Value.ConnectionString);

        DefaultTypeMap.MatchNamesWithUnderscores = true;
        SqlMapper.AddTypeHandler(new TransactionHashTypeHandler());
    }

    public void Dispose()
    {
        _dataSource.Dispose();
    }

    public async Task Store(StreamTransaction @event)
    {
        using var connection = _dataSource.CreateConnection();

        try
        {
            await connection.ExecuteAsync(
                "INSERT INTO transactions(transaction_hash, stream_id, stream_index, payload) VALUES (@transactionHash, @streamId, @streamIndex, @payload)",
                @event);
        }
        catch (Exception e) when (e.Message.Contains("Invalid stream index"))
        {
            throw new OutOfOrderException($"The transaction on stream {@event.StreamId} has an invalid stream index.");
        }
    }

    public async Task<Registry.V1.Block?> GetBlock(TransactionHash transactionHash)
    {
        using var connection = _dataSource.CreateConnection();

        var id = await GetTransactionId(connection, transactionHash);
        if (id == null)
            return null;

        var block = await connection.QuerySingleOrDefaultAsync<BlockRecord>(
            "SELECT * FROM blocks WHERE from_transaction <= @id AND @id <= to_transaction",
            new { id });

        if (block is null)
            return null;

        if (block.Publication is null)
            return null;

        return new Registry.V1.Block
        {
            Header = new Registry.V1.BlockHeader
            {
                PreviousHeaderHash = ByteString.CopyFrom(block.PreviousHeaderHash),
                PreviousPublicationHash = ByteString.CopyFrom(block.PreviousPublicationHash),
                MerkleRootHash = ByteString.CopyFrom(block.MerkleRootHash),
                CreatedAt = block.CreatedAt.ToTimestamp(),
            },
            Publication = Registry.V1.BlockPublication.Parser.ParseFrom(block.Publication)
        };
    }

    public async Task<IList<StreamTransaction>> GetStreamTransactionsForBlock(BlockHash blockHash)
    {
        using var connection = _dataSource.CreateConnection();

        var blockInfo = await connection.QuerySingleAsync<(long FromTransaction, long ToTransaction)>(
              "SELECT from_transaction, to_transaction FROM blocks WHERE block_hash = @blockHash",
              new { blockHash = blockHash.Data });

        var events = await connection.QueryAsync<StreamTransaction>(
            "SELECT transaction_hash, stream_id, stream_index, payload FROM transactions WHERE @from_transaction <= id AND id <= @to_transaction ORDER BY ID ASC",
            blockInfo);

        return events.AsList();
    }

    public async Task<IList<StreamTransaction>> GetStreamTransactionsForStream(Guid streamId)
    {
        await using var connection = _dataSource.CreateConnection();

        var events = await connection.QueryAsync<StreamTransaction>(
            "SELECT transaction_hash, stream_id, stream_index, payload FROM transactions WHERE stream_id = @streamId ORDER BY id ASC",
            new { streamId });

        return events.AsList();
    }

    public async Task<TransactionStatus> GetTransactionStatus(TransactionHash transactionHash)
    {
        await using var connection = _dataSource.CreateConnection();

        var id = await GetTransactionId(connection, transactionHash);
        if (id == null)
            return TransactionStatus.Unknown;

        var hasBeenPublished = await connection.QuerySingleOrDefaultAsync<bool?>(
            "SELECT publication IS NOT NULL FROM blocks WHERE from_transaction <= @id AND @id <= to_transaction",
            new { id });

        if (hasBeenPublished.HasValue && hasBeenPublished.Value)
            return TransactionStatus.Committed;

        return TransactionStatus.Pending;
    }

    public async Task<NewBlock?> CreateNextBlock()
    {
        using var connection = _dataSource.CreateConnection();
        await connection.OpenAsync();
        using var transaction = await connection.BeginTransactionAsync();

        try
        {
            // create table lock on blocks table to prevent concurrent block creation
            await connection.ExecuteAsync("LOCK TABLE blocks IN EXCLUSIVE MODE");

            var previousBlock = await connection.QuerySingleOrDefaultAsync<BlockRecord>(
                "SELECT * FROM blocks ORDER BY to_transaction DESC LIMIT 1");

            if (previousBlock is not null && previousBlock.Publication is null)
                throw new InvalidOperationException("Previous block has not been published");

            var fromTransaction = (previousBlock?.ToTransaction ?? 0) + 1;
            var maxTransactionId = await connection.QuerySingleOrDefaultAsync<long?>("SELECT MAX(id) FROM transactions");

            if (maxTransactionId is null || maxTransactionId.Value < fromTransaction)
                return null;

            var toTransaction = fromTransaction + BlockSizeCalculator.CalculateBlockLength(maxTransactionId.Value - fromTransaction + 1) - 1;

            var transactions = (await connection.QueryAsync<StreamTransaction>(
                "SELECT transaction_hash, stream_id, stream_index, payload FROM transactions WHERE @fromTransaction <= id AND id <= @toTransaction ORDER BY ID ASC",
                new { fromTransaction, toTransaction })).ToList();

            var merkleRootHash = transactions.CalculateMerkleRoot(x => x.Payload);
            var previousHeaderHash = previousBlock?.BlockHash ?? new byte[32];
            var previousPublicationHash = previousBlock is not null ? SHA256.HashData(previousBlock!.Publication!) : new byte[32];

            var blockHeader = new Registry.V1.BlockHeader
            {
                PreviousHeaderHash = ByteString.CopyFrom(previousHeaderHash),
                PreviousPublicationHash = ByteString.CopyFrom(previousPublicationHash),
                MerkleRootHash = ByteString.CopyFrom(merkleRootHash),
                CreatedAt = Timestamp.FromDateTimeOffset(DateTimeOffset.UtcNow)
            };

            await connection.ExecuteAsync(
                "INSERT INTO blocks(block_hash, previous_header_hash, previous_publication_hash, merkle_root_hash, created_at, from_transaction, to_transaction) VALUES (@blockHash, @previousHeaderHash, @previousPublicationHash, @merkleRootHash, @createdAt, @fromTransaction, @toTransaction)",
                new BlockRecord
                {
                    BlockHash = SHA256.HashData(blockHeader.ToByteArray()),
                    PreviousHeaderHash = previousHeaderHash,
                    PreviousPublicationHash = previousPublicationHash,
                    MerkleRootHash = merkleRootHash,
                    CreatedAt = DateTimeOffset.UtcNow,
                    FromTransaction = fromTransaction,
                    ToTransaction = toTransaction,
                    Publication = null,
                });

            await transaction.CommitAsync();

            return new(blockHeader, transactions.Select(x => x.TransactionHash).ToList());
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task FinalizeBlock(BlockHash hash, Registry.V1.BlockPublication publication)
    {
        using var connection = _dataSource.CreateConnection();

        // update block with publication and return number of rows affected
        var affectedRows = await connection.ExecuteAsync(
            "UPDATE blocks SET publication = @publication WHERE block_hash = @blockHash AND (publication IS NULL OR publication = @publication)",
            new { blockHash = hash.Data, publication = publication.ToByteArray() });

        if (affectedRows == 0)
            throw new InvalidOperationException("Block not found or already published or publication does not match");
    }

    private static Task<long?> GetTransactionId(IDbConnection connection, TransactionHash transactionHash)
    {
        return connection.QuerySingleOrDefaultAsync<long?>(
              "SELECT id FROM transactions WHERE transaction_hash = @transactionHash",
              new { transactionHash = transactionHash.Data });
    }

    public async Task<IList<Block>> GetBlocks(int skip, int take, bool includeTransactions)
    {
        using var connection = _dataSource.CreateConnection();

        var blocks = await connection.QueryAsync<BlockRecord>(
                "SELECT * FROM blocks ORDER BY id ASC OFFSET @skip LIMIT @take", new { skip, take });

        IEnumerable<BlockTransaction>? transactions = null;

        if (includeTransactions)
        {
            var start = blocks.Min(x => x.FromTransaction);
            var end = blocks.Max(x => x.ToTransaction);
            transactions = (await connection.QueryAsync<BlockTransaction>(
                "SELECT  id, payload FROM transactions WHERE @start <= id AND id <= @end ORDER BY ID ASC",
                new { start, end })).ToList();
        }

        var mappedBlocks = blocks.Select(x =>
        {
            var block = new Block
            {
                Header = new BlockHeader
                {
                    PreviousHeaderHash = ByteString.CopyFrom(x.PreviousHeaderHash),
                    PreviousPublicationHash = ByteString.CopyFrom(x.PreviousPublicationHash),
                    MerkleRootHash = ByteString.CopyFrom(x.MerkleRootHash),
                    CreatedAt = x.CreatedAt.ToTimestamp(),
                },
                Publication = x.Publication is not null ? BlockPublication.Parser.ParseFrom(x.Publication) : null,
                Height = x.Id,
            };

            if (transactions is not null)
            {
                block.Transactions.AddRange(
                    transactions
                        .Where(t => x.FromTransaction <= t.Id && t.Id <= x.ToTransaction)
                        .Select(t => Transaction.Parser.ParseFrom(t.Payload)
                ));
            }

            return block;
        }).ToList();

        return mappedBlocks;
    }

    private sealed record BlockTransaction
    {
        public required long Id { get; init; }
        public required byte[] Payload { get; init; }
    }

    private sealed record BlockRecord
    {
        public int Id { get; init; }
        public required byte[] BlockHash { get; init; }
        public required byte[] PreviousHeaderHash { get; init; }
        public required byte[] PreviousPublicationHash { get; init; }
        public required byte[] MerkleRootHash { get; init; }
        public required DateTimeOffset CreatedAt { get; init; }
        public required long FromTransaction { get; init; }
        public required long ToTransaction { get; init; }
        public byte[]? Publication { get; init; }
    }
}
