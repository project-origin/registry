using ProjectOrigin.RequestProcessor.Services;
using ProjectOrigin.RequestProcessor.Tests.ExampleChat;
using ProjectOrigin.VerifiableEventStore.Models;
using ProjectOrigin.VerifiableEventStore.Services.EventStore;

namespace ProjectOrigin.Services.Tests;

public class ModelLoaderTests
{
    [Fact]
    public async Task ModelLoader_GetDummyChat_Success()
    {
        var fixture = new Fixture();
        var eventStreamId = Guid.NewGuid();
        var serializer = new JsonEventSerializer();

        var events = new List<Event>(){
            serializer.Serialize(new(eventStreamId, 0), fixture.Create<ChatCreatedEvent>()),
            serializer.Serialize(new(eventStreamId, 1), fixture.Create<MessagePostedEvent>()),
            serializer.Serialize(new(eventStreamId, 2), fixture.Create<MessagePostedEvent>())
        };

        var eventStoreMock = new Mock<IEventStore>();
        eventStoreMock.Setup(obj => obj.GetEventsForEventStream(It.IsAny<Guid>())).ReturnsAsync(events);

        var modelLoader = new ModelLoader(eventStoreMock.Object, new JsonEventSerializer());

        var (chat, count) = await modelLoader.Get(eventStreamId, typeof(Chat));

        Assert.IsType<Chat>(chat);
        var dummyChat = chat as Chat;
        Assert.Equal(events.Count(), count);
        Assert.Equal(events.Count(), dummyChat!.Events.Count);
    }

    [Fact]
    public async Task ModelLoader_NoEvents_ReturnsNull()
    {
        var fixture = new Fixture();
        var eventStreamId = Guid.NewGuid();
        var serializer = new JsonEventSerializer();

        var events = new List<Event>();

        var eventStoreMock = new Mock<IEventStore>();
        eventStoreMock.Setup(obj => obj.GetEventsForEventStream(It.IsAny<Guid>())).ReturnsAsync(events);

        var modelLoader = new ModelLoader(eventStoreMock.Object, new JsonEventSerializer());

        var (chat, count) = await modelLoader.Get(eventStreamId, typeof(Chat));

        Assert.Null(chat);
    }
}
