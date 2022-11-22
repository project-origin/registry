using Google.Protobuf;
using Microsoft.Extensions.Options;
using ProjectOrigin.Register.StepProcessor.Models;
using ProjectOrigin.Register.StepProcessor.Services;
using ProjectOrigin.Register.StepProcessor.Tests.ExampleChat;
using ProjectOrigin.VerifiableEventStore.Models;
using ProjectOrigin.VerifiableEventStore.Services.EventStore;

namespace ProjectOrigin.Services.Tests;

public class ModelLoaderTests
{
    private byte[] Serialize(IMessage message)
    {
        using var ms = new MemoryStream();
        message.WriteTo(ms);
        return ms.ToArray();
    }

    [Fact]
    public async Task ModelLoader_GetDummyChat_Success()
    {
        var fixture = new Fixture();
        var eventStreamId = Guid.NewGuid();
        var thisRegister = fixture.Create<string>();

        var events = new List<VerifiableEvent>(){
            new (new(eventStreamId, 0), new SignedEvent(fixture.Create<ChatCreatedEvent>(), ByteString.Empty.ToArray()).Serialize()),
            new (new(eventStreamId, 1), new SignedEvent(fixture.Create<MessagePostedEvent>(), ByteString.Empty.ToArray()).Serialize()),
            new (new(eventStreamId, 2), new SignedEvent(fixture.Create<MessagePostedEvent>(), ByteString.Empty.ToArray()).Serialize())
        };

        var optionsMock = CreateOptionsMock(thisRegister, events);

        var modelLoader = new ModelLoader(optionsMock);

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
        var thisRegister = fixture.Create<string>();

        var events = new List<VerifiableEvent>();

        var optionsMock = CreateOptionsMock(thisRegister, events);
        var modelLoader = new ModelLoader(optionsMock);

        var (chat, count) = await modelLoader.Get(new(thisRegister, eventStreamId), typeof(Chat));

        Assert.Null(chat);
    }

    [Fact]
    public async Task ModelLoader_InvalidRegistry_ThrowsException()
    {
        var fixture = new Fixture();
        var eventStreamId = Guid.NewGuid();
        var thisRegister = fixture.Create<string>();
        var otherRegister = fixture.Create<string>();

        var events = new List<VerifiableEvent>();
        var optionsMock = CreateOptionsMock(thisRegister, events);
        var modelLoader = new ModelLoader(optionsMock);

        var ex = await Assert.ThrowsAsync<NullReferenceException>(() => modelLoader.Get(new(otherRegister, eventStreamId), typeof(Chat)));
        Assert.Equal($"Connection to EventStore for registry ”{otherRegister}” could not be found", ex.Message);
    }

    private IOptions<ModelLoaderOptions> CreateOptionsMock(string registry, List<VerifiableEvent> events)
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
