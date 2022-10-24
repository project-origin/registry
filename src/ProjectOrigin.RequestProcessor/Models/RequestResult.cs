namespace ProjectOrigin.RequestProcessor.Models;

public record RequestResult(RequestId Id, RequestState State, string? ErrorMessage = null);
