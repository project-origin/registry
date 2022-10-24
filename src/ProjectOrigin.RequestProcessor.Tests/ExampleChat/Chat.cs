using ProjectOrigin.RequestProcessor.Interfaces;

namespace ProjectOrigin.RequestProcessor.Tests.ExampleChat;

public class Chat : IModel, IModelProjectable<ChatCreatedEvent>, IModelProjectable<MessagePostedEvent>
{
    public static Chat? Null
    {
        get
        {
            return null;
        }
    }

    public List<object> Events { get; } = new();

    public void Apply(MessagePostedEvent e) => Events.Add(e);
    public void Apply(ChatCreatedEvent e) => Events.Add(e);
}
