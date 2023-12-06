using System;

namespace ProjectOrigin.VerifiableEventStore.Services.EventStore
{
    [Serializable]
    public class OutOfOrderException : Exception
    {
        public OutOfOrderException(string? message) : base(message)
        {
        }
    }
}
