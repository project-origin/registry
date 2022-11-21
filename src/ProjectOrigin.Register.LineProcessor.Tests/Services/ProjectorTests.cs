using Google.Protobuf;
using ProjectOrigin.Register.LineProcessor.Services;
using ProjectOrigin.Register.LineProcessor.Tests.ExampleChat;

namespace ProjectOrigin.Services.Tests;

public class ProjectorTests
{
    [Fact]
    public void Projector_ProjectEvents_Success()
    {
        var fixture = new Fixture();
        var projector = new ModelProjector(typeof(Chat));

        var events = new List<IMessage>(){
            fixture.Create<ChatCreatedEvent>(),
            fixture.Create<MessagePostedEvent>(),
            fixture.Create<MessagePostedEvent>()
        };

        var projection = projector.Project(events);

        Assert.IsType<Chat>(projection);
        var dummyChat = projection as Chat;

        Assert.Equal(events, dummyChat!.Events);
    }

    [Fact]
    public void Projector_ProjectEventsInvalidType_Failure()
    {
        var fixture = new Fixture();
        var projector = new ModelProjector(typeof(Chat));

        var events = new List<IMessage>(){
            fixture.Create<ChatCreatedEvent>(),
            fixture.Create<UnsupportedEvent>(),
            fixture.Create<MessagePostedEvent>()
        };

        var ex = Assert.Throws<NotImplementedException>(() => projector.Project(events));
        Assert.Equal($"No ”Apply” method implemented on class ”{nameof(Chat)}” for event ”{nameof(UnsupportedEvent)}”", ex.Message);
    }
}
