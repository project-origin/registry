using Microsoft.Extensions.Options;
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
        var thisRegister = fixture.Create<string>();

        var events = new List<Event>(){
            serializer.Serialize(new(eventStreamId, 0), fixture.Create<ChatCreatedEvent>()),
            serializer.Serialize(new(eventStreamId, 1), fixture.Create<MessagePostedEvent>()),
            serializer.Serialize(new(eventStreamId, 2), fixture.Create<MessagePostedEvent>())
        };

        var optionsMock = CreateOptionsMock(thisRegister, events);

        var modelLoader = new ModelLoader(optionsMock, serializer);

        var (chat, count) = await modelLoader.Get(new(thisRegister, eventStreamId), typeof(Chat));

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
        var thisRegister = fixture.Create<string>();

        var events = new List<Event>();

        var optionsMock = CreateOptionsMock(thisRegister, events);
        var modelLoader = new ModelLoader(optionsMock, serializer);

        var (chat, count) = await modelLoader.Get(new(thisRegister, eventStreamId), typeof(Chat));

        Assert.Null(chat);
    }

    [Fact]
    public async Task ModelLoader_InvalidRegistry_ThrowsException()
    {
        var fixture = new Fixture();
        var eventStreamId = Guid.NewGuid();
        var serializer = new JsonEventSerializer();
        var thisRegister = fixture.Create<string>();
        var otherRegister = fixture.Create<string>();

        var events = new List<Event>();
        var optionsMock = CreateOptionsMock(thisRegister, events);
        var modelLoader = new ModelLoader(optionsMock, serializer);

        var ex = await Assert.ThrowsAsync<NullReferenceException>(() => modelLoader.Get(new(otherRegister, eventStreamId), typeof(Chat)));
        Assert.Equal($"Connection to EventStore for registry ”{otherRegister}” could not be found", ex.Message);
    }

    private IOptions<ModelLoaderOptions> CreateOptionsMock(string registry, List<Event> events)
    {

        var eventStoreMock = new Mock<IEventStore>();
        eventStoreMock.Setup(obj => obj.GetEventsForEventStream(It.IsAny<Guid>())).ReturnsAsync(events);

        var dictionary = new Dictionary<string, IEventStore>()
        {
            {registry, eventStoreMock.Object},
        };


        var optionsMock = new Mock<IOptions<ModelLoaderOptions>>();
        optionsMock.Setup(obj => obj.Value).Returns(new ModelLoaderOptions(dictionary));
        return optionsMock.Object;
    }

}
