using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using AutoFixture;
using FluentAssertions;
using Google.Protobuf;
using ProjectOrigin.VerifiableEventStore.Extensions;
using ProjectOrigin.VerifiableEventStore.Models;
using ProjectOrigin.VerifiableEventStore.Services.EventStore;
using ProjectOrigin.VerifiableEventStore.Services.Repository;
using Xunit;

namespace ProjectOrigin.VerifiableEventStore.Tests.Repository;

public abstract class AbstractTransactionRepositoryTests<T> where T : ITransactionRepository
{
    protected Fixture _fixture;
    protected abstract T Repository { get; }

    public AbstractTransactionRepositoryTests()
    {
        _fixture = new Fixture();
    }

    private StreamTransaction CreateFakeEvent(Guid stream, int index)
    {
        var payload = _fixture.Create<byte[]>();

        var transaction = new Registry.V1.Transaction
        {
            Header = new Registry.V1.TransactionHeader
            {
                FederatedStreamId = new Common.V1.FederatedStreamId
                {
                    Registry = _fixture.Create<string>(),
                    StreamId = new Common.V1.Uuid()
                    {
                        Value = stream.ToString()
                    }
                },
                Nonce = _fixture.Create<string>(),
                PayloadSha512 = ByteString.CopyFrom(SHA512.HashData(payload)),
                PayloadType = _fixture.Create<string>()
            },
            HeaderSignature = ByteString.CopyFrom(_fixture.Create<byte[]>()),
            Payload = ByteString.CopyFrom(payload),
        };

        var transactionBytes = transaction.ToByteArray();

        return new StreamTransaction
        {
            TransactionHash = new TransactionHash(SHA256.HashData(transactionBytes)),
            StreamId = stream,
            StreamIndex = index,
            Payload = transactionBytes
        };
    }

    [Fact]
    public async Task InsertSingleEvent_InOrder_Success()
    {
        var @event = CreateFakeEvent(Guid.NewGuid(), 0);

        await Repository.Store(@event);

        var eventStream = await Repository.GetStreamTransactionsForStream(@event.StreamId);
        var fromDatabase = eventStream.First();

        fromDatabase.Should().BeEquivalentTo(@event);
    }

    [Fact]
    public async Task InsertEvents_DifferentStreams_Success()
    {
        const int NUMBER_OF_EVENTS = 150;

        List<StreamTransaction> events = new();
        for (var i = 0; i < NUMBER_OF_EVENTS; i++)
        {
            var @event = CreateFakeEvent(Guid.NewGuid(), 0);
            events.Add(@event);
            await Repository.Store(@event);
        }

        var firstEvent = events.First();
        var eventStream = await Repository.GetStreamTransactionsForStream(firstEvent.StreamId);

        eventStream.Should().ContainSingle();
        eventStream.Should().ContainEquivalentOf(firstEvent);
    }

    [Fact]
    public async Task InsertManyEvents_SameStream_Success()
    {
        const int NUMBER_OF_EVENTS = 1500;
        var streamId = Guid.NewGuid();

        for (var i = 0; i < NUMBER_OF_EVENTS; i++)
        {
            var @event = CreateFakeEvent(streamId, i);
            await Repository.Store(@event);
        }

        var events = await Repository.GetStreamTransactionsForStream(streamId);
        Assert.NotEmpty(events);
        Assert.Equal(NUMBER_OF_EVENTS, events.Count);
    }

    [Fact]
    public async Task InsertTwoEvent_SameStream_OutOfOrderException()
    {
        var streamId = Guid.NewGuid();

        await Repository.Store(CreateFakeEvent(streamId, 0));
        var @event = CreateFakeEvent(streamId, 3);

        async Task act() => await Repository.Store(@event);
        await Assert.ThrowsAnyAsync<OutOfOrderException>(act);
    }

    [Fact]
    public async Task InsertEvent_OutOfOrder_ThrowsException()
    {
        var streamId = Guid.NewGuid();

        var @event = CreateFakeEvent(streamId, 99);

        async Task act() => await Repository.Store(@event);
        await Assert.ThrowsAnyAsync<OutOfOrderException>(act);
    }

    [Fact]
    public async Task Will_Store_EventAsync()
    {
        var @event = CreateFakeEvent(Guid.NewGuid(), 0);

        await Repository.Store(@event);

        var eventStream = await Repository.GetStreamTransactionsForStream(@event.StreamId);
        Assert.NotEmpty(eventStream);
    }

    [Fact]
    public async Task GetBlockFromTransactionHash_NoneExistingBlock_ReturnNull()
    {
        var transactionHash = new Fixture().Create<TransactionHash>();
        var block = await Repository.GetBlock(transactionHash);

        Assert.Null(block);
    }

    [Fact]
    public async Task CreateNextBlock_NoTransaction_NullBlock()
    {
        // Given

        // When
        var newBlock = await Repository.CreateNextBlock();

        // Then
        newBlock.Should().BeNull();
    }

    [Fact]
    public async Task CreateNextBlock_NoNewTransactions_NullBlock()
    {
        // Given
        await Repository.Store(CreateFakeEvent(Guid.NewGuid(), 0));
        var block1 = await Repository.CreateNextBlock();
        block1.Should().NotBeNull();
        block1!.TransactionHashes.Should().HaveCount(1);
        await Repository.FinalizeBlock(BlockHash.FromHeader(block1!.Header), new Registry.V1.BlockPublication());

        // When
        var newBlock = await Repository.CreateNextBlock();

        // Then
        newBlock.Should().BeNull();
    }

    [Fact]
    public async Task CreateNextBlock_NotFinalized_ThrowsException()
    {
        // Given
        await Repository.Store(CreateFakeEvent(Guid.NewGuid(), 0));
        var block1 = await Repository.CreateNextBlock();
        block1.Should().NotBeNull();
        block1!.TransactionHashes.Should().HaveCount(1);

        // When
        async Task act() => await Repository.CreateNextBlock();

        // Then
        var ex = await Assert.ThrowsAnyAsync<InvalidOperationException>(act);
        ex.Message.Should().BeEquivalentTo("Previous block has not been published");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(8)]
    [InlineData(32)]
    [InlineData(150)]
    [InlineData(1024)]
    public async Task Can_Create_Next_Block_For_Finalization(int numberOfTransaction)
    {
        // Given
        var transactions = new List<StreamTransaction>();
        foreach (var i in Enumerable.Range(0, numberOfTransaction))
        {
            var @event = CreateFakeEvent(Guid.NewGuid(), 0);
            await Repository.Store(@event);
            transactions.Add(@event);
        }

        // When
        var newBlock = await Repository.CreateNextBlock();

        // Then
        newBlock.Should().NotBeNull();
        newBlock!.TransactionHashes.Should().HaveCount(numberOfTransaction);
        newBlock!.Header.PreviousHeaderHash.ToArray().Should().BeEquivalentTo(new byte[32]);
        newBlock!.Header.MerkleRootHash.Should().BeEquivalentTo(transactions.CalculateMerkleRoot(x => x.Payload));
    }

    [Fact]
    public async Task Can_FinalizeBlock()
    {
        for (var i = 0; i < 100; i++)
        {
            var @event = CreateFakeEvent(Guid.NewGuid(), 0);
            await Repository.Store(@event);
        }

        var newBlock = await Repository.CreateNextBlock();

        newBlock.Should().NotBeNull();

        var publication = new Registry.V1.BlockPublication
        {
            LogEntry = new Registry.V1.BlockPublication.Types.LogEntry
            {
                BlockHeaderHash = ByteString.CopyFrom(SHA256.HashData(newBlock!.Header.ToByteArray())),
            }
        };

        await Repository.FinalizeBlock(BlockHash.FromHeader(newBlock!.Header), publication);
    }

    [Fact]
    public async Task CanCreateSeriesOfBlocks()
    {
        var transactionList1 = new List<StreamTransaction>();
        for (var i = 0; i < 10; i++)
        {
            var @event = CreateFakeEvent(Guid.NewGuid(), 0);
            transactionList1.Add(@event);
            await Repository.Store(@event);
        }
        var block1 = await Repository.CreateNextBlock();
        block1.Should().NotBeNull();
        block1!.Header.PreviousHeaderHash.Should().BeEquivalentTo(new byte[32]);
        block1!.Header.PreviousPublicationHash.Should().BeEquivalentTo(new byte[32]);
        block1!.TransactionHashes.Should().HaveCount(transactionList1.Count);
        block1!.TransactionHashes.Should().ContainInOrder(transactionList1.Select(x => x.TransactionHash));
        var pub1 = new Registry.V1.BlockPublication();
        await Repository.FinalizeBlock(BlockHash.FromHeader(block1!.Header), pub1);


        var transactionList2 = new List<StreamTransaction>();
        for (var i = 0; i < 10; i++)
        {
            var @event = CreateFakeEvent(Guid.NewGuid(), 0);
            transactionList2.Add(@event);
            await Repository.Store(@event);
        }
        var block2 = await Repository.CreateNextBlock();
        block2.Should().NotBeNull();
        block2!.Header.PreviousHeaderHash.Should().BeEquivalentTo(SHA256.HashData(block1!.Header.ToByteArray()));
        block2!.Header.PreviousPublicationHash.Should().BeEquivalentTo(SHA256.HashData(pub1.ToByteArray()));
        block2!.TransactionHashes.Should().HaveCount(transactionList2.Count);
        block2!.TransactionHashes.Should().ContainInOrder(transactionList2.Select(x => x.TransactionHash));
        var pub2 = new Registry.V1.BlockPublication();
        await Repository.FinalizeBlock(BlockHash.FromHeader(block2!.Header), pub2);


        var transactionList3 = new List<StreamTransaction>();
        for (var i = 0; i < 10; i++)
        {
            var @event = CreateFakeEvent(Guid.NewGuid(), 0);
            transactionList3.Add(@event);
            await Repository.Store(@event);
        }
        var block3 = await Repository.CreateNextBlock();
        block3.Should().NotBeNull();
        block3!.Header.PreviousHeaderHash.Should().BeEquivalentTo(SHA256.HashData(block2!.Header.ToByteArray()));
        block3!.Header.PreviousPublicationHash.Should().BeEquivalentTo(SHA256.HashData(pub2.ToByteArray()));
        block3!.TransactionHashes.Should().HaveCount(transactionList3.Count);
        block3!.TransactionHashes.Should().ContainInOrder(transactionList3.Select(x => x.TransactionHash));
        var pub3 = new Registry.V1.BlockPublication();
        await Repository.FinalizeBlock(BlockHash.FromHeader(block3!.Header), pub3);
    }

    [Fact]
    public async Task CanGetBlocks()
    {
        // Arrange
        for (var b = 0; b < 10; b++)
        {
            for (var t = 0; t < 10 * (b + 1); t++)
            {
                var @event = CreateFakeEvent(Guid.NewGuid(), 0);
                await Repository.Store(@event);
            }
            var block = await Repository.CreateNextBlock();
            await Repository.FinalizeBlock(BlockHash.FromHeader(block!.Header), new Registry.V1.BlockPublication());
        }

        // Act
        var blocks1 = await Repository.GetBlocks(0, 2, false);

        // Assert
        blocks1.Should().HaveCount(2);
        blocks1[0].Header.Should().NotBeNull();
        blocks1[1].Header.Should().NotBeNull();

        blocks1[0].Height.Should().Be(1);
        blocks1[0].Transactions.Should().BeEmpty();
        blocks1[1].Height.Should().Be(2);
        blocks1[0].Transactions.Should().BeEmpty();

        // Act
        var blocks2 = await Repository.GetBlocks(6, 2, true);
        blocks2.Should().HaveCount(2);
        blocks2[0].Header.Should().NotBeNull();
        blocks1[1].Header.Should().NotBeNull();

        blocks2[0].Height.Should().Be(7);
        blocks2[0].Transactions.Should().HaveCount(70);
        blocks2[1].Height.Should().Be(8);
        blocks2[1].Transactions.Should().HaveCount(80);

    }
}
