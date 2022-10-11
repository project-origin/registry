using EnergyOrigin.VerifiableEventStore.Services.BlockchainConnector;
using Microsoft.Extensions.Options;

namespace EnergyOrigin.VerifiableEventStore.Tests;

public class ConcordiumTests
{
    [Fact]
    [Trait("Category", "Concordium")]
    public async Task GetRequiredHashes_ValidateNumberOfHashesReturned()
    {
        var knownTransactionHash = "f8931b7da1f1464453d78e8ab606dee44b3bca00c91170e2fcbcba552da485f4";
        var knownBlockId = "06a531f87594658eee1aeb369b3e755e5b5bb6a34501aa5d24e2adfa025e7343";

        var optionsMock = new Mock<IOptions<ConcordiumOptions>>();
        optionsMock.Setup(obj => obj.Value).Returns(new ConcordiumOptions("http://localhost:10001", "rpcadmin", "", ""));
        var connector = new ConcordiumConnector(optionsMock.Object);

        var block = await connector.GetBlock(new TransactionReference(knownTransactionHash));

        Assert.NotNull(block);
        Assert.True(block?.Final);
        Assert.Equal(knownBlockId, block?.BlockId);
    }
}
