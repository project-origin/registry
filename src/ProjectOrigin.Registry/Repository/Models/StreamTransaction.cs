using System;

namespace ProjectOrigin.VerifiableEventStore.Models;

public record StreamTransaction
{
    public required TransactionHash TransactionHash { get; init; }
    public required Guid StreamId { get; init; }
    public required int StreamIndex { get; init; }
    public required byte[] Payload { get; init; }
}
