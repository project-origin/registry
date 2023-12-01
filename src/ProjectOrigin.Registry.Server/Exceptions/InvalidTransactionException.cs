using System;
using System.Runtime.Serialization;

namespace ProjectOrigin.Registry.Server.Exceptions;

[Serializable]
public class InvalidTransactionException : Exception
{
    public InvalidTransactionException()
    {
    }

    public InvalidTransactionException(string? message) : base(message)
    {
    }

    public InvalidTransactionException(string? message, Exception? innerException) : base(message, innerException)
    {
    }

    protected InvalidTransactionException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}
