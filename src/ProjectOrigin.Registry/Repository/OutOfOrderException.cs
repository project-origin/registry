using System;

namespace ProjectOrigin.VerifiableEventStore.Services.EventStore;

public class OutOfOrderException : Exception
{
    public OutOfOrderException(string? message) : base(message)
    {
    }
}
