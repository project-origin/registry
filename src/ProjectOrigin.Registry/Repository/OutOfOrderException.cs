using System;

namespace ProjectOrigin.Registry.Repository;

public class OutOfOrderException : Exception
{
    public OutOfOrderException(string? message) : base(message)
    {
    }
}
