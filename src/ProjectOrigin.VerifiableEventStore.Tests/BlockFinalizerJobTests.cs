using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using FluentAssertions;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Moq;
using ProjectOrigin.VerifiableEventStore.Models;
using ProjectOrigin.VerifiableEventStore.Services.BlockFinalizer;
using ProjectOrigin.VerifiableEventStore.Services.BlockPublisher;
using ProjectOrigin.VerifiableEventStore.Services.Repository;
using ProjectOrigin.VerifiableEventStore.Services.TransactionStatusCache;
using Xunit;

namespace ProjectOrigin.VerifiableEventStore.Tests;

public class BlockFinalizerJobTests
{
    private readonly Fixture _fixture;
    private readonly Mock<ILogger<BlockFinalizerJob>> _logger;
    private readonly Mock<IBlockPublisher> _blockPublisher;
    private readonly Mock<ITransactionStatusService> _statusService;
    private readonly Mock<ITransactionRepository> _repository;
    private readonly BlockFinalizerJob _job;

    public BlockFinalizerJobTests()
    {
        _fixture = new Fixture();
        _logger = new Mock<ILogger<BlockFinalizerJob>>();
        _blockPublisher = new Mock<IBlockPublisher>();
        _statusService = new Mock<ITransactionStatusService>();
        _repository = new Mock<ITransactionRepository>();
        _job = new BlockFinalizerJob(_logger.Object, _blockPublisher.Object, _repository.Object, _statusService.Object);
    }

    [Fact]
    public async Task Success()
    {
        // Given
        var header = new ImmutableLog.V1.BlockHeader
        {
            PreviousHeaderHash = ByteString.CopyFrom(new byte[32]),
            PreviousPublicationHash = ByteString.CopyFrom(new byte[32]),
            MerkleRootHash = ByteString.CopyFrom(new byte[32]),
            CreatedAt = Timestamp.FromDateTime(DateTime.UtcNow)
        };

        var publication = new ImmutableLog.V1.BlockPublication
        {
            LogEntry = new ImmutableLog.V1.BlockPublication.Types.LogEntry
            {
                BlockHeaderHash = ByteString.CopyFrom(SHA256.HashData(header.ToByteArray())),
            }
        };

        var transactions = _fixture.Create<List<TransactionHash>>();

        _repository.Setup(obj => obj.CreateNextBlock()).ReturnsAsync(new NewBlock(header, transactions));
        _blockPublisher.Setup(obj => obj.PublishBlock(It.IsAny<ImmutableLog.V1.BlockHeader>())).Returns(Task.FromResult(publication));

        // When
        await _job.Execute(CancellationToken.None);

        // Then
        _repository.Verify(obj => obj.CreateNextBlock(), Times.Exactly(1));
        _blockPublisher.Verify(obj => obj.PublishBlock(It.Is<ImmutableLog.V1.BlockHeader>(x => x == header)), Times.Exactly(1));
        _repository.Verify(obj => obj.FinalizeBlock(It.Is<BlockHash>(x => x.Equals(BlockHash.FromHeader(header))), It.Is<ImmutableLog.V1.BlockPublication>(x => x == publication)), Times.Exactly(1));
        _statusService.Verify(obj => obj.SetTransactionStatus(It.IsAny<TransactionHash>(), It.Is<TransactionStatusRecord>(x => x.NewStatus == Models.TransactionStatus.Committed)), Times.Exactly(transactions.Count));
    }

    [Fact]
    public async Task NoNewBlocks()
    {
        // Given
        NewBlock? a = null;
        _repository.Setup(obj => obj.CreateNextBlock()).ReturnsAsync(a);

        // When
        await _job.Execute(CancellationToken.None);

        // Then
        _repository.Verify(obj => obj.CreateNextBlock(), Times.Exactly(1));
        _blockPublisher.VerifyNoOtherCalls();
        _repository.VerifyNoOtherCalls();
        _statusService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task NotPublishedException()
    {
        // Given
        var exception = new InvalidOperationException("Previous block has not been published");
        _repository.Setup(obj => obj.CreateNextBlock()).ThrowsAsync(exception);

        // When

        async Task act() => await _job.Execute(CancellationToken.None);
        var thrownException = await Assert.ThrowsAsync<InvalidOperationException>(act);

        // Then
        _repository.Verify(obj => obj.CreateNextBlock(), Times.Exactly(1));
        thrownException.Message.Should().Be(exception.Message);
        _blockPublisher.VerifyNoOtherCalls();
        _repository.VerifyNoOtherCalls();
        _statusService.VerifyNoOtherCalls();
    }
}
