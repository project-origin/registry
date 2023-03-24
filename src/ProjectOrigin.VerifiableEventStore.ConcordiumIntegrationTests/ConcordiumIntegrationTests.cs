using Microsoft.Extensions.Options;
using Moq;
using ProjectOrigin.VerifiableEventStore.Services.BlockchainConnector;
using ProjectOrigin.VerifiableEventStore.Services.BlockchainConnector.Concordium;
using Xunit;

namespace ProjectOrigin.VerifiableEventStore.ConcordiumIntegrationTests;

public class ConcordiumIntegrationTests
{
    const string NodeAddress = "http://testnet-node:10001";
    const string NodeToken = "rpcadmin";
    const int FiveSeconds = 5000;

    [Fact]
    public async Task GetRandomKnownTransaction_Success()
    {
        var randomKnownTransactionHash = "f8931b7da1f1464453d78e8ab606dee44b3bca00c91170e2fcbcba552da485f4";
        var knownBlockId = "06a531f87594658eee1aeb369b3e755e5b5bb6a34501aa5d24e2adfa025e7343";

        var connector = GetConcordiumConnector();

        var block = await connector.GetBlock(new TransactionReference(randomKnownTransactionHash));

        Assert.NotNull(block);
        Assert.True(block?.Final);
        Assert.Equal(knownBlockId, block?.BlockId);
    }

    [Fact]
    public async Task PublishHelloWorld_Success()
    {
        var connector = GetConcordiumConnector();

        var transactionRef = await connector.PublishBytes(System.Text.Encoding.UTF8.GetBytes("Hello world"));

        Assert.NotNull(transactionRef);
    }

    [Fact]
    public async Task PublishAndWaitForTransaction_Success()
    {
        var connector = GetConcordiumConnector();

        var transactionRef = await connector.PublishBytes(System.Text.Encoding.UTF8.GetBytes("Hello world"));

        Block? block;
        var i = 0;
        do
        {
            await Task.Delay(FiveSeconds);
            block = await connector.GetBlock(transactionRef);
        }
        while (block is null && i++ < 10);

        Assert.NotNull(block);
        Assert.True(block?.Final);
    }

    private ConcordiumConnector GetConcordiumConnector()
    {
        var accountAddress = GetEnvironmentVariable("AccountAddress");
        var accountKey = GetEnvironmentVariable("AccountKey");

        var optionsMock = new Mock<IOptions<ConcordiumOptions>>();
        optionsMock.Setup(obj => obj.Value).Returns(new ConcordiumOptions()
        {
            Address = NodeAddress,
            AuthenticationToken = NodeToken,
            AccountAddress = accountAddress,
            AccountKey = accountKey
        });

        return new ConcordiumConnector(optionsMock.Object);
    }

    private string GetEnvironmentVariable(string name)
    {
        return Environment.GetEnvironmentVariable(name) ?? throw new ArgumentException($"Environment variable ”{name}” does not exist!");
    }
}
