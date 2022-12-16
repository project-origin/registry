using System.Runtime.Serialization;

namespace ProjectOrigin.VerifiableEventStore.Services.EventStore.Postgres
{
    [Serializable]
    public class OutOfOrderException : Exception
    {
        public OutOfOrderException()
        {
        }

        public OutOfOrderException(string? message) : base(message)
        {
        }

        public OutOfOrderException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        protected OutOfOrderException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
