using System;

namespace ProjectOrigin.Electricity.Example.Exceptions;

public class InvalidTransactionException : Exception
{
    public InvalidTransactionException(string message) : base(message)
    {
    }
}
