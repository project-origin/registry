using Microsoft.Extensions.Options;
using ProjectOrigin.Register.StepProcessor.Interfaces;
using ProjectOrigin.Register.StepProcessor.Models;
using ProjectOrigin.Register.StepProcessor.Services;
using ProjectOrigin.VerifiableEventStore.Models;

namespace ProjectOrigin.Register.StepProcessor.Tests;

public class SynchronousCommandStepProcessorTests
{
    [Fact]
    public async Task RequestProcessor_QueueRequest_Success()
    {
        var fixture = new Fixture();
        var registryName = fixture.Create<string>();
        var index = fixture.Create<int>();
        var request = NewRequest(registryName);

        var federatedEventStoreMock = MockFederatedEventStore(index);
        var dispatcherMock = new Mock<ICommandStepVerifiere>();
        dispatcherMock.Setup(obj => obj.Verify(It.IsAny<V1.CommandStep>(), It.IsAny<Dictionary<V1.FederatedStreamId, IEnumerable<V1.SignedEvent>>>())).ReturnsAsync(new VerificationResult.Valid());
        var optionsMock = CreateOptions(registryName);
        var processor = new CommandStepProcessor(optionsMock, federatedEventStoreMock.Object, dispatcherMock.Object);

        await processor.Process(request);

        var streamId = Guid.Parse(request.RoutingId.StreamId.Value);

        federatedEventStoreMock.Verify(obj => obj.PublishEvent(It.Is<VerifiableEvent>(e => e.Id == new EventId(streamId, index))), Times.Once);
    }

    [Fact]
    public async Task RequestProcessor_GetStatus_Error()
    {
        var fixture = new Fixture();
        var registryName = fixture.Create<string>();
        var request = NewRequest(registryName);

        var errorMessage = fixture.Create<string>();

        var federatedEventStoreMock = MockFederatedEventStore(1);
        var dispatcherMock = new Mock<ICommandStepVerifiere>();
        dispatcherMock.Setup(obj => obj.Verify(It.IsAny<V1.CommandStep>(), It.IsAny<Dictionary<V1.FederatedStreamId, IEnumerable<V1.SignedEvent>>>())).ReturnsAsync(new VerificationResult.Invalid(errorMessage));
        var optionsMock = CreateOptions(registryName);

        var processor = new CommandStepProcessor(optionsMock, federatedEventStoreMock.Object, dispatcherMock.Object);
        var result = await processor.Process(request);

        Assert.Equal(V1.CommandState.Failed, result.State);
        Assert.Equal(errorMessage, result.Error);
    }

    [Fact]
    public async Task RequestProcessor_InvalidRegistry_ThrowsException()
    {
        var fixture = new Fixture();
        var registryName = fixture.Create<string>();
        var otherRegistryName = fixture.Create<string>();
        var request = NewRequest(otherRegistryName);
        var errorMessage = fixture.Create<string>();

        var federatedEventStoreMock = MockFederatedEventStore(1);
        var dispatcherMock = new Mock<ICommandStepVerifiere>();
        dispatcherMock.Setup(obj => obj.Verify(It.IsAny<V1.CommandStep>(), It.IsAny<Dictionary<V1.FederatedStreamId, IEnumerable<V1.SignedEvent>>>())).ReturnsAsync(new VerificationResult.Valid());
        var optionsMock = CreateOptions(registryName);

        var processor = new CommandStepProcessor(optionsMock, federatedEventStoreMock.Object, dispatcherMock.Object);
        var ex = await Assert.ThrowsAsync<InvalidDataException>(() => processor.Process(request));

        Assert.Equal("Invalid registry for request", ex.Message);
    }

    private Mock<IFederatedEventStore> MockFederatedEventStore(int i)
    {
        var fixture = new Fixture();
        var federatedEventStoreMock = new Mock<IFederatedEventStore>();
        federatedEventStoreMock.Setup(obj => obj.GetStreams((It.IsAny<IEnumerable<V1.FederatedStreamId>>()))).Returns<IEnumerable<V1.FederatedStreamId>>(
            (fids) =>
            {
                IDictionary<V1.FederatedStreamId, IEnumerable<V1.SignedEvent>> dictionary = fids.Select(x => (fid: x, events: fixture.CreateMany<V1.SignedEvent>(i)))
                    .ToDictionary(x => x.fid, x => x.events);

                return Task.FromResult(dictionary);
            });

        return federatedEventStoreMock;
    }

    private IOptions<CommandStepProcessorOptions> CreateOptions(string registryName)
    {
        return CreateOptionsMock<CommandStepProcessorOptions>(new CommandStepProcessorOptions { RegistryName = registryName });
    }

    private IOptions<T> CreateOptionsMock<T>(T content) where T : class
    {
        var optionsMock = new Mock<IOptions<T>>();
        optionsMock.Setup(obj => obj.Value).Returns(content);
        return optionsMock.Object;
    }

    private static V1.CommandStep NewRequest(string registry)
    {
        var fixture = new Fixture();
        return new V1.CommandStep()
        {
            RoutingId = new V1.FederatedStreamId
            {
                Registry = registry,
                StreamId = new V1.Uuid
                {
                    Value = Guid.NewGuid().ToString()
                }
            },
            SignedEvent = fixture.Create<V1.SignedEvent>(),
        };
    }
}
