using NSec.Cryptography;
using Xunit;
using Moq;
using EnergyOrigin.VerifiableEventStore.Services.BlockchainConnector;
using Microsoft.Extensions.Options;

namespace EnergyOrigin.VerifiableEventStore.ConcordiumIntegrationTests;

public class ConcordiumIntegrationTests
{
    const string NodeAddress = "http://testnet-node:10001";
    const string NodeToken = "rpcadmin";
    const string FakeAccount = "MmU1ZTdlYjYzOWJmODc0MjNiYWM1Nzk2Y2ViMWY3MGU4MDU5Mz";
    const string FakePrivateKey = "2e5e7eb639bf87423bac5796ceb1f70e805e938803e36154e361892214974926";
    const int FiveSeconds = 5000;

    [Fact]
    public async Task GetRandomKnownTransaction_Success()
    {
        var randomKnownTransactionHash = "f8931b7da1f1464453d78e8ab606dee44b3bca00c91170e2fcbcba552da485f4";
        var knownBlockId = "06a531f87594658eee1aeb369b3e755e5b5bb6a34501aa5d24e2adfa025e7343";

        var optionsMock = new Mock<IOptions<ConcordiumOptions>>();
        optionsMock.Setup(obj => obj.Value).Returns(new ConcordiumOptions(NodeAddress, NodeToken, FakeAccount, FakePrivateKey));
        var connector = new ConcordiumConnector(optionsMock.Object);

        var block = await connector.GetBlock(new TransactionReference(randomKnownTransactionHash));

        Assert.NotNull(block);
        Assert.True(block?.Final);
        Assert.Equal(knownBlockId, block?.BlockId);
    }

    [Fact]
    public async Task PublishHelloWorld_Success()
    {
        var accountAddress = Environment.GetEnvironmentVariable("AccountAddress");
        var accountKey = Environment.GetEnvironmentVariable("AccountKey");

        var optionsMock = new Mock<IOptions<ConcordiumOptions>>();
        optionsMock.Setup(obj => obj.Value).Returns(new ConcordiumOptions(NodeAddress, NodeToken, accountAddress, accountKey));
        var connector = new ConcordiumConnector(optionsMock.Object);

        var transactionRef = await connector.PublishBytes(System.Text.Encoding.UTF8.GetBytes("Hello world"));

        Assert.NotNull(transactionRef);
    }

    [Fact]
    public async Task PublishAndWaitForTransaction_Success()
    {
        var accountAddress = Environment.GetEnvironmentVariable("AccountAddress");
        var accountKey = Environment.GetEnvironmentVariable("AccountKey");

        var optionsMock = new Mock<IOptions<ConcordiumOptions>>();
        optionsMock.Setup(obj => obj.Value).Returns(new ConcordiumOptions(NodeAddress, NodeToken, accountAddress, accountKey));
        var connector = new ConcordiumConnector(optionsMock.Object);

        var transactionRef = await connector.PublishBytes(System.Text.Encoding.UTF8.GetBytes("Hello world"));

        Block? b;
        var i = 0;
        do
        {
            await Task.Delay(FiveSeconds);
            b = await connector.GetBlock(transactionRef);
        }
        while (b is null && i++ < 10);

        Assert.NotNull(b);
        Assert.True(b?.Final);
    }
}
