using Google.Protobuf;
using ProjectOrigin.Register.StepProcessor.Services;
using ProjectOrigin.VerifiableEventStore.Models;
using ProjectOrigin.VerifiableEventStore.Services.BatchProcessor;
using ProjectOrigin.VerifiableEventStore.Services.EventStore;

namespace ProjectOrigin.Register.StepProcessor.Tests;

public class InProcessFederatedEventStoreTests
{
    [Fact]
    public async Task PublishEvent_Success()
    {
        var fixture = new Fixture();
        var batcherMock = new Mock<IEventStore>();

        var federatedEventStore = new InProcessFederatedEventStore(batcherMock.Object, new());
        var @event = fixture.Create<VerifiableEvent>();

        await federatedEventStore.PublishEvent(@event);

        batcherMock.Verify(obj => obj.Store(It.Is<VerifiableEvent>(e => e == @event)), Times.Once);
    }

    [Fact]
    public async Task GetStreams_Success()
    {
        var fixture = new Fixture();
        var registryName = fixture.Create<string>();
        var streamId = Guid.NewGuid();
        var eventStoreMock = new Mock<IEventStore>();
        var federatedId = CreateFederatedStreamId(registryName, streamId);

        var expectedResult = fixture.CreateMany<V1.SignedEvent>();
        var verifiableEvents = expectedResult.Select((e, i) => new VerifiableEvent(new EventId(streamId, i), e.ToByteArray()));
        eventStoreMock.Setup(x => x.GetEventsForEventStream(streamId)).ReturnsAsync(verifiableEvents);

        var federatedEventStore = new InProcessFederatedEventStore(null!, new(){
            {registryName, eventStoreMock.Object}
        });

        var result = await federatedEventStore.GetStreams(new V1.FederatedStreamId[] { federatedId });

        Assert.Contains(federatedId, result);
        Assert.Equal(expectedResult, result[federatedId]);
    }


    [Fact]
    public async Task GetStreams_Unknown()
    {
        var fixture = new Fixture();
        var registryName = fixture.Create<string>();
        var streamId = Guid.NewGuid();

        var federatedId = CreateFederatedStreamId(registryName, streamId);
        var federatedEventStore = new InProcessFederatedEventStore(null!, new() { });

        var ex = await Assert.ThrowsAsync<NullReferenceException>(() => federatedEventStore.GetStreams(new V1.FederatedStreamId[] { federatedId }));
        Assert.Equal($"Connection to EventStore for registry ”{registryName}” could not be found", ex.Message);
    }

    private static V1.FederatedStreamId CreateFederatedStreamId(string registryName, Guid streamId) => new V1.FederatedStreamId
    {
        Registry = registryName,
        StreamId = new V1.Uuid { Value = streamId.ToString() }
    };
}
