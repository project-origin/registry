using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using ProjectOrigin.VerifiableEventStore.Services.BlockchainConnector.Concordium;
using Xunit;

namespace ProjectOrigin.VerifiableEventStore.Tests;

public class ConcordiumTests
{
    const string NodeAddress = "http://testnet-node:20001";
    const string NodeToken = "rpcadmin";
    const string FakeAccount = "MmU1ZTdlYjYzOWJmODc0MjNiYWM1Nzk2Y2ViMWY3MGU4MDU5Mz";
    const string FakePrivateKey = "2e5e7eb639bf87423bac5796ceb1f70e805e938803e36154e361892214974926";

    [Fact]
    public void ConcodiumConnector_Instanciate_Success()
    {
        Mock<ILogger<ConcordiumPublisher>> logMock = new();
        var options = Options.Create(new ConcordiumOptions()
        {
            Address = NodeAddress,
            AuthenticationToken = NodeToken,
            AccountAddress = FakeAccount,
            AccountKey = FakePrivateKey
        });
        var connector = new ConcordiumPublisher(logMock.Object, options);

        Assert.NotNull(connector);
    }

    [Fact]
    public void ConcodiumConnector_InvalidKey_Fails()
    {
        Mock<ILogger<ConcordiumPublisher>> logMock = new();
        var options = Options.Create(new ConcordiumOptions()
        {
            Address = NodeAddress,
            AuthenticationToken = NodeToken,
            AccountAddress = FakeAccount,
            AccountKey = "invalidkey"
        });

        Assert.ThrowsAny<ArgumentException>(() => new ConcordiumPublisher(logMock.Object, options));
    }
}
