using Google.Protobuf;
using Microsoft.Extensions.Options;
using ProjectOrigin.Register.StepProcessor.Interfaces;
using ProjectOrigin.Register.StepProcessor.Models;
using ProjectOrigin.Register.StepProcessor.Services;
using ProjectOrigin.VerifiableEventStore.Models;
using ProjectOrigin.VerifiableEventStore.Services.Batcher;
using ProjectOrigin.VerifiableEventStore.Services.EventStore;

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

        var batcherMock = new Mock<IBatcher>();
        var dispatcherMock = new Mock<ICommandStepVerifier>();
        dispatcherMock.Setup(obj => obj.Verify(It.IsAny<V1.CommandStep>(), It.IsAny<Dictionary<V1.FederatedStreamId, IEnumerable<V1.SignedEvent>>>())).ReturnsAsync(new VerificationResult.Valid());
        var optionsMock = CreateOptions(registryName, registryName, index);

        var processor = new CommandStepProcessor(optionsMock, dispatcherMock.Object, batcherMock.Object);

        await processor.Process(request);

        var streamId = Guid.Parse(request.RoutingId.StreamId.Value);

        batcherMock.Verify(obj => obj.PublishEvent(It.Is<VerifiableEvent>(e => e.Id == new EventId(streamId, index))), Times.Once);
    }

    [Fact]
    public async Task RequestProcessor_GetStatus_Error()
    {
        var fixture = new Fixture();
        var registryName = fixture.Create<string>();
        var request = NewRequest(registryName);

        var errorMessage = fixture.Create<string>();

        var batcherMock = new Mock<IBatcher>();
        var dispatcherMock = new Mock<ICommandStepVerifier>();
        dispatcherMock.Setup(obj => obj.Verify(It.IsAny<V1.CommandStep>(), It.IsAny<Dictionary<V1.FederatedStreamId, IEnumerable<V1.SignedEvent>>>())).ReturnsAsync(new VerificationResult.Invalid(errorMessage));
        var optionsMock = CreateOptions(registryName, registryName, 1);

        var processor = new CommandStepProcessor(optionsMock, dispatcherMock.Object, batcherMock.Object);
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

        var batcherMock = new Mock<IBatcher>();
        var dispatcherMock = new Mock<ICommandStepVerifier>();
        dispatcherMock.Setup(obj => obj.Verify(It.IsAny<V1.CommandStep>(), It.IsAny<Dictionary<V1.FederatedStreamId, IEnumerable<V1.SignedEvent>>>())).ReturnsAsync(new VerificationResult.Valid());
        var optionsMock = CreateOptions(registryName, registryName, 1);

        var processor = new CommandStepProcessor(optionsMock, dispatcherMock.Object, batcherMock.Object);
        var ex = await Assert.ThrowsAsync<InvalidDataException>(() => processor.Process(request));

        Assert.Equal("Invalid registry for request", ex.Message);
    }

    [Fact]
    public async Task RequestProcessor_CouldNotFindEventStore_ThrowsException()
    {
        var fixture = new Fixture();
        var registryName = fixture.Create<string>();
        var eventStoreName = fixture.Create<string>();
        var request = NewRequest(registryName);
        var errorMessage = fixture.Create<string>();

        var batcherMock = new Mock<IBatcher>();
        var dispatcherMock = new Mock<ICommandStepVerifier>();
        dispatcherMock.Setup(obj => obj.Verify(It.IsAny<V1.CommandStep>(), It.IsAny<Dictionary<V1.FederatedStreamId, IEnumerable<V1.SignedEvent>>>())).ReturnsAsync(new VerificationResult.Valid());
        var optionsMock = CreateOptions(registryName, eventStoreName, 1);

        var processor = new CommandStepProcessor(optionsMock, dispatcherMock.Object, batcherMock.Object);
        var ex = await Assert.ThrowsAsync<NullReferenceException>(() => processor.Process(request));

        Assert.Equal($"Connection to EventStore for registry ”{registryName}” could not be found", ex.Message);
    }

    private IOptions<CommandStepProcessorOptions> CreateOptions(string registryName, string eventStoreName, int i)
    {
        var fixture = new Fixture();

        var b = fixture.CreateMany<V1.SignedEvent>(i);
        var c = b.Select(x => new VerifiableEvent(fixture.Create<EventId>(), x.ToByteArray()));

        var eventStoreMock = new Mock<IEventStore>();
        eventStoreMock.Setup(obj => obj.GetEventsForEventStream(It.IsAny<Guid>())).ReturnsAsync(c);
        return CreateOptionsMock<CommandStepProcessorOptions>(new CommandStepProcessorOptions(registryName, new Dictionary<string, IEventStore>()
        {
            {eventStoreName, eventStoreMock.Object}
        }));
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
