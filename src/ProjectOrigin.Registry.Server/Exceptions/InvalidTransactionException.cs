using System;

namespace ProjectOrigin.Registry.Server.Exceptions;

[Serializable]
public class InvalidTransactionException : Exception
{
    public InvalidTransactionException(string? message) : base(message)
    {
    }
}
