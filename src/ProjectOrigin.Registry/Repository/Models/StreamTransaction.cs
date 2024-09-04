using System;

namespace ProjectOrigin.Registry.Repository.Models;

public record StreamTransaction
{
    public required TransactionHash TransactionHash { get; init; }
    public required Guid StreamId { get; init; }
    public required int StreamIndex { get; init; }
    public required byte[] Payload { get; init; }
}
