using System;

namespace ProjectOrigin.Registry.ChartTests.Exceptions;

public class InvalidTransactionException : Exception
{
    public InvalidTransactionException(string message) : base(message)
    {
    }
}
