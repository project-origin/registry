using ProjectOrigin.RequestProcessor.Services;
using ProjectOrigin.RequestProcessor.Tests.ExampleChat;
using ProjectOrigin.VerifiableEventStore.Models;

namespace ProjectOrigin.Services.Tests;

public class JsonEventSerializerTests
{
    [Fact]
    public void JsonEventSerializer_Success()
    {
        var fixture = new Fixture();

        var eventId = fixture.Create<EventId>();
        var eventInput = fixture.Create<MessagePostedEvent>();

        var serializer = new JsonEventSerializer();

        var serializedEvent = serializer.Serialize(eventId, eventInput);

        var deserializedObject = serializer.Deserialize(serializedEvent);

        Assert.IsType<MessagePostedEvent>(deserializedObject);

        var eventOutput = deserializedObject as MessagePostedEvent;
        Assert.Equal(eventInput, eventOutput);
    }
}
