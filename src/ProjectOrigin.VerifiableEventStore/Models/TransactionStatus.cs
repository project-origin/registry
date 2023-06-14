namespace ProjectOrigin.VerifiableEventStore.Models;

public enum TransactionStatus
{
    Unknown = 0,
    Pending = 1,
    Failed = 2,
    Committed = 3
}
