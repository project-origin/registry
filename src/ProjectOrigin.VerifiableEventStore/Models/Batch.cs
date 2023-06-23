using System;

namespace ProjectOrigin.VerifiableEventStore.Models;

public record Batch(Guid Id, Guid PreviousBatchId, string BlockId, string TransactionId)
{
    public bool IsFinalized => !string.IsNullOrEmpty(TransactionId);
}
