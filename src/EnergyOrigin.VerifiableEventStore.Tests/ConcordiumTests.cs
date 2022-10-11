using EnergyOrigin.VerifiableEventStore.Services.BlockchainConnector;
using Microsoft.Extensions.Options;
using NSec.Cryptography;

namespace EnergyOrigin.VerifiableEventStore.Tests;

public class ConcordiumTests
{
    const string NodeAddress = "http://testnet-node:10001";
    const string NodeToken = "rpcadmin";
    const string FakeAccount = "MmU1ZTdlYjYzOWJmODc0MjNiYWM1Nzk2Y2ViMWY3MGU4MDU5Mz";
    const string FakePrivateKey = "2e5e7eb639bf87423bac5796ceb1f70e805e938803e36154e361892214974926";

    [Fact]
    public void ConcodiumConnector_Instanciate_Success()
    {
        var optionsMock = new Mock<IOptions<ConcordiumOptions>>();
        optionsMock.Setup(obj => obj.Value).Returns(new ConcordiumOptions(NodeAddress, NodeToken, FakeAccount, FakePrivateKey));
        var connector = new ConcordiumConnector(optionsMock.Object);

        Assert.NotNull(connector);
    }

    [Fact]
    public void ConcodiumConnector_InvalidKey_Fails()
    {
        var optionsMock = new Mock<IOptions<ConcordiumOptions>>();
        optionsMock.Setup(obj => obj.Value).Returns(new ConcordiumOptions(NodeAddress, NodeToken, FakeAccount, "invalidkey"));

        Assert.ThrowsAny<ArgumentException>(() => new ConcordiumConnector(optionsMock.Object));
    }

    [Fact(Skip = "Should only run on integration test")]
    [Trait("Category", "Concordium")]
    public async Task GetRequiredHashes_ValidateNumberOfHashesReturned()
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
}
