using System;
using System.Threading.Tasks;
using AutoFixture;
using FluentAssertions;
using ProjectOrigin.VerifiableEventStore.Models;
using ProjectOrigin.VerifiableEventStore.Services.EventStore.InMemory;
using ProjectOrigin.VerifiableEventStore.Services.TransactionStatusCache;
using Xunit;

namespace ProjectOrigin.VerifiableEventStore.Tests.TransactionStatusCache;

public abstract class AbstractTransactionStatusServiceTests
{
    protected Fixture _fixture;
    protected InMemoryRepository _repository;

    protected abstract ITransactionStatusService Service { get; }

    public AbstractTransactionStatusServiceTests()
    {
        _fixture = new Fixture();
        _repository = new InMemoryRepository();
    }

    [Fact]
    public async Task ShouldReturnUnknownTransactionStatus()
    {
        // Arrange
        var transactionHash = _fixture.Create<TransactionHash>();

        // Act
        var record = await Service.GetTransactionStatus(transactionHash);

        // Assert
        record.NewStatus.Should().Be(TransactionStatus.Unknown);
    }

    [Fact]
    public async Task ShouldReturnSetStatus()
    {
        // Arrange
        var transactionHash = _fixture.Create<TransactionHash>();
        var transactionStatusRecord = _fixture.Create<TransactionStatusRecord>();

        await Service.SetTransactionStatus(transactionHash, transactionStatusRecord);

        // Act
        var record = await Service.GetTransactionStatus(transactionHash);

        // Assert
        record.Should().BeEquivalentTo(transactionStatusRecord);
    }

    [Fact]
    public async Task ShouldNotSetLowerStatus()
    {
        // Arrange
        var transactionHash = _fixture.Create<TransactionHash>();

        await Service.SetTransactionStatus(transactionHash, new TransactionStatusRecord(TransactionStatus.Committed));
        await Service.SetTransactionStatus(transactionHash, new TransactionStatusRecord(TransactionStatus.Pending));

        // Act
        var record = await Service.GetTransactionStatus(transactionHash);

        // Assert
        record.NewStatus.Should().Be(TransactionStatus.Committed);
    }

    [Fact]
    public async Task ShouldReturnPendingRecordFromRepositoryNotInBlock()
    {
        // Arrange
        var data = _fixture.Create<byte[]>();
        var transactionHash = new TransactionHash(data);
        await _repository.Store(new StreamTransaction
        {
            TransactionHash = transactionHash,
            StreamId = Guid.NewGuid(),
            StreamIndex = 0,
            Payload = data
        });

        // Act
        var record = await Service.GetTransactionStatus(transactionHash);

        // Assert
        record.NewStatus.Should().Be(TransactionStatus.Pending);
    }

    [Fact]
    public async Task ShouldReturnCommittedRecordFromRepositoryInBlock()
    {
        // Arrange
        var data = _fixture.Create<byte[]>();
        var transactionHash = new TransactionHash(data);
        await _repository.Store(new StreamTransaction
        {
            TransactionHash = transactionHash,
            StreamId = Guid.NewGuid(),
            StreamIndex = 0,
            Payload = data
        });

        var newBlock = await _repository.CreateNextBlock();
        await _repository.FinalizeBlock(BlockHash.FromHeader(newBlock!.Header), new ImmutableLog.V1.BlockPublication());

        // Act
        var record = await Service.GetTransactionStatus(transactionHash);

        // Assert
        record.NewStatus.Should().Be(TransactionStatus.Committed);
    }
}
