using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.Arm;
using System.Security.Cryptography;
using System.Threading.Tasks;
using FluentAssertions;
using Google.Protobuf;
using ProjectOrigin.VerifiableEventStore.Extensions;
using ProjectOrigin.VerifiableEventStore.Models;
using ProjectOrigin.VerifiableEventStore.Services.EventStore;

namespace ProjectOrigin.VerifiableEventStore.Tests;

public abstract class AbstractEventStoreTests<T> where T : IEventStore
{
    protected const int MaxBatchExponent = 10;
    protected Fixture _fixture;
    protected abstract T EventStore { get; }

    public AbstractEventStoreTests()
    {
        _fixture = new Fixture();
    }

    private VerifiableEvent CreateFakeEvent(Guid stream, int index)
    {
        var transaction = _fixture.Create<byte[]>();
        return new VerifiableEvent { TransactionHash = new TransactionHash(SHA256.HashData(transaction)), StreamId = stream, StreamIndex = index, Payload = transaction };
    }

    [Fact]
    public async Task InsertSingleEvent_InOrder_Success()
    {
        var @event = CreateFakeEvent(Guid.NewGuid(), 0);

        await EventStore.Store(@event);

        var eventStream = await EventStore.GetEventsForEventStream(@event.StreamId);
        var fromDatabase = eventStream.First();

        fromDatabase.Should().BeEquivalentTo(@event);
    }

    [Fact]
    public async Task InsertEvents_DifferentStreams_Success()
    {
        const int NUMBER_OF_EVENTS = 150;

        List<VerifiableEvent> events = new();
        for (var i = 0; i < NUMBER_OF_EVENTS; i++)
        {
            var @event = CreateFakeEvent(Guid.NewGuid(), 0);
            events.Add(@event);
            await EventStore.Store(@event);
        }

        var firstEvent = events.First();
        var eventStream = await EventStore.GetEventsForEventStream(firstEvent.StreamId);

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
            await EventStore.Store(@event);
        }

        var events = await EventStore.GetEventsForEventStream(streamId);
        Assert.NotEmpty(events);
        Assert.Equal(NUMBER_OF_EVENTS, events.Count());
    }

    [Fact]
    public async Task InsertTwoEvent_SameStream_OutOfOrderException()
    {
        var streamId = Guid.NewGuid();

        await EventStore.Store(CreateFakeEvent(streamId, 0));
        var @event = CreateFakeEvent(streamId, 3);

        async Task act() => await EventStore.Store(@event);
        await Assert.ThrowsAnyAsync<OutOfOrderException>(act);
    }

    [Fact]
    public async Task InsertEvent_OutOfOrder_ThrowsException()
    {
        var streamId = Guid.NewGuid();

        var @event = CreateFakeEvent(streamId, 99);

        async Task act() => await EventStore.Store(@event);
        await Assert.ThrowsAnyAsync<OutOfOrderException>(act);
    }

    [Fact]
    public async Task Will_Store_EventAsync()
    {
        var @event = CreateFakeEvent(Guid.NewGuid(), 0);

        await EventStore.Store(@event);

        var eventStream = await EventStore.GetEventsForEventStream(@event.StreamId);
        Assert.NotEmpty(eventStream);
    }

    [Fact]
    public async Task GetBatchFromTransactionHash_NoneExistingBatch_ReturnNull()
    {
        var transactionHash = new Fixture().Create<TransactionHash>();
        var batchResult = await EventStore.GetBatchFromTransactionHash(transactionHash);

        Assert.Null(batchResult);
    }

    [Theory]
    [InlineData(10, 10)]
    [InlineData(16, 16)]
    [InlineData(18, 18)]
    [InlineData(34, 34)]
    [InlineData(68, 68)]
    [InlineData(128, 128)]
    [InlineData((1 << MaxBatchExponent) + 10, 1 << MaxBatchExponent)]
    public async Task Can_Get_Batches_For_Finalization(int numberOfTransaction, int numberInBlock)
    {
        var sentTransactions = new List<VerifiableEvent>();

        for (var i = 0; i < numberOfTransaction; i++)
        {
            var @event = CreateFakeEvent(Guid.NewGuid(), 0);
            sentTransactions.Add(@event);
            await EventStore.Store(@event);
        }

        var (header, transactions) = await EventStore.CreateNextBatch();
        var root = sentTransactions.CalculateMerkleRoot(x => x.Payload);

        transactions.Should().HaveCount(numberInBlock);
        header.PreviousHeaderHash.ToArray().Should().BeEquivalentTo(new byte[32]);
    }

    [Fact]
    public async Task Can_FinalizeBatch()
    {
        for (var i = 0; i < 100; i++)
        {
            var @event = CreateFakeEvent(Guid.NewGuid(), 0);
            await EventStore.Store(@event);
        }

        var (header, _) = await EventStore.CreateNextBatch();

        var publication = new ImmutableLog.V1.BlockPublication
        {
            LogEntry = new ImmutableLog.V1.BlockPublication.Types.LogEntry
            {
                BatchHeaderHash = ByteString.CopyFrom(SHA256.HashData(header.ToByteArray())),
            }
        };

        await EventStore.FinalizeBatch(BatchHash.FromHeader(header), publication);
    }
}
