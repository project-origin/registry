using System;

namespace ProjectOrigin.Registry.Exceptions;

public class InvalidTransactionException : Exception
{
    public InvalidTransactionException(string? message) : base(message)
    {
    }

    public InvalidTransactionException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
