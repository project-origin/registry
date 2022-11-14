namespace ProjectOrigin.VerifiableEventStore.Models;

public sealed partial class EventId
{
    public EventId(Guid eventStreamId, int index)
    {
        this.EventStreamId = new()
        {
            Value = eventStreamId.ToString()
        };
        Index = (uint)index;
    }
}
