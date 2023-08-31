using System;
using System.Threading.Tasks;
using FluentAssertions;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using ProjectOrigin.ImmutableLog.V1;
using ProjectOrigin.VerifiableEventStore.Services.BlockchainConnector.Concordium;
using Xunit;

namespace ProjectOrigin.VerifiableEventStore.ConcordiumIntegrationTests;

public class ConcordiumIntegrationTests
{
    const string NodeAddress = "http://testnet-node:20001";
    const string NodeToken = "rpcadmin";
    private readonly ConcordiumPublisher _publisher;

    public ConcordiumIntegrationTests()
    {
        Mock<ILogger<ConcordiumPublisher>> logMock = new();
        var options = Options.Create(new ConcordiumOptions()
        {
            Address = NodeAddress,
            AuthenticationToken = NodeToken,
            AccountAddress = GetRequiredEnvironmentVariable("AccountAddress"),
            AccountKey = GetRequiredEnvironmentVariable("AccountKey")
        });

        _publisher = new ConcordiumPublisher(logMock.Object, options);
    }

    [Fact]
    public async Task PublishAndWaitForTransaction_Success()
    {
        var header = new BlockHeader
        {
            PreviousHeaderHash = ByteString.CopyFrom(new byte[32]),
            PreviousPublicationHash = ByteString.CopyFrom(new byte[32]),
            MerkleRootHash = ByteString.CopyFrom(System.Text.Encoding.UTF8.GetBytes("Hello world")),
            CreatedAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(DateTime.UtcNow)
        };

        var blockPublication = await _publisher.PublishBlock(header);

        blockPublication.Concordium.TransactionHash.Length.Should().BeGreaterThan(0);
        blockPublication.Concordium.BlockHash.Length.Should().BeGreaterThan(0);
    }

    private static string GetRequiredEnvironmentVariable(string name)
    {
        return Environment.GetEnvironmentVariable(name) ?? throw new ArgumentException($"Environment variable ”{name}” does not exist!");
    }
}
