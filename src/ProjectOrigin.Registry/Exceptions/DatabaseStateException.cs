using System;

namespace ProjectOrigin.Registry.Exceptions;

public class DatabaseStateException : Exception
{
    public DatabaseStateException(string? message) : base(message)
    {
    }

    public DatabaseStateException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
