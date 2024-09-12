using ProjectOrigin.Registry.Repository.Models;

namespace ProjectOrigin.Registry.TransactionStatusCache;

public record TransactionStatusRecord(TransactionStatus NewStatus, string Message = "");
