using System;

namespace ProjectOrigin.Registry.Exceptions;

public class InvalidConfigurationException : Exception
{
    public InvalidConfigurationException(string? message) : base(message)
    {
    }

    public InvalidConfigurationException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
