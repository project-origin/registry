namespace ProjectOrigin.VerifiableEventStore.Models;

public record TransactionStatusRecord(TransactionStatus NewStatus, string Message = "");
