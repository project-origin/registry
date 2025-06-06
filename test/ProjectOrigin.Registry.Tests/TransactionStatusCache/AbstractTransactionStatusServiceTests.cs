using System;
using System.Threading.Tasks;
using AutoFixture;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ProjectOrigin.Registry.Repository.InMemory;
using ProjectOrigin.Registry.Repository.Models;
using ProjectOrigin.Registry.TransactionStatusCache;
using Xunit;

namespace ProjectOrigin.Registry.Tests.TransactionStatusCache;

public abstract class AbstractTransactionStatusServiceTests
{
    protected Fixture _fixture;
    protected InMemoryRepository _repository;

    protected abstract ITransactionStatusService Service { get; }
    protected abstract IInvocationList LoggedMessages { get; }

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
    public async Task ShouldReturnSetStatusMultipleTimesWithoutWarning()
    {
        // Arrange
        var transactionHash = _fixture.Create<TransactionHash>();

        // Act
        await Service.SetTransactionStatus(transactionHash, new TransactionStatusRecord(TransactionStatus.Unknown));
        await Service.SetTransactionStatus(transactionHash, new TransactionStatusRecord(TransactionStatus.Pending));
        await Service.SetTransactionStatus(transactionHash, new TransactionStatusRecord(TransactionStatus.Committed));
        await Service.SetTransactionStatus(transactionHash, new TransactionStatusRecord(TransactionStatus.Finalized));

        var record = await Service.GetTransactionStatus(transactionHash);

        // Assert
        record.Should().BeEquivalentTo(new TransactionStatusRecord(TransactionStatus.Finalized));
        LoggedMessages
            .Should()
            .NotContain(x => x.Method.Name == nameof(ILogger.Log) && x.Arguments[0].Equals(LogLevel.Warning));
    }

    [Theory]
    [InlineData(TransactionStatus.Committed, TransactionStatus.Pending)]
    [InlineData(TransactionStatus.Pending, TransactionStatus.Unknown)]
    [InlineData(TransactionStatus.Finalized, TransactionStatus.Committed)]
    public async Task ShouldNotSetLowerStatus(TransactionStatus firstState, TransactionStatus tryState)
    {
        // Arrange
        var transactionHash = _fixture.Create<TransactionHash>();

        await Service.SetTransactionStatus(transactionHash, new TransactionStatusRecord(firstState));
        await Service.SetTransactionStatus(transactionHash, new TransactionStatusRecord(tryState));

        // Act
        var record = await Service.GetTransactionStatus(transactionHash);

        // Assert
        record.NewStatus.Should().Be(firstState);
    }

    [Fact]
    public async Task ShouldReturnCommittedRecordFromRepositoryNotInBlock()
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
        record.NewStatus.Should().Be(TransactionStatus.Committed);
    }

    [Fact]
    public async Task ShouldReturnFinalizedRecordFromRepositoryInBlock()
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
        await _repository.FinalizeBlock(BlockHash.FromHeader(newBlock!.Header), new Registry.V1.BlockPublication());

        // Act
        var record = await Service.GetTransactionStatus(transactionHash);

        // Assert
        record.NewStatus.Should().Be(TransactionStatus.Finalized);
    }
}
