using System;

namespace ProjectOrigin.Registry.Server.Exceptions;

public class InvalidTransactionException : Exception
{
    public InvalidTransactionException(string? message) : base(message)
    {
    }
}
