using System;

namespace ProjectOrigin.VerifiableEventStore.Models;

public record EventId(Guid EventStreamId, int Index)
{
    public override string ToString() => $"{EventStreamId}-{Index}";
}
