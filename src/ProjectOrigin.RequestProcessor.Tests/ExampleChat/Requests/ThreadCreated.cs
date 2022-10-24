using ProjectOrigin.RequestProcessor.Interfaces;
using ProjectOrigin.RequestProcessor.Models;

namespace ProjectOrigin.RequestProcessor.Tests.ExampleChat;

public record ChatCreatedEvent(Guid topic);

public record ChatCreatedRequest(FederatedStreamId id, ChatCreatedEvent e) : PublishRequest<ChatCreatedEvent>(id, new byte[0], e);

public class ChatCreatedVerifier : IRequestVerifier<ChatCreatedRequest, Chat>
{
    public Task<VerificationResult> Verify(ChatCreatedRequest request, Chat? model)
    {
        if (model != null)
            return VerificationResult.Invalid("Invalid request, chat already exists.");

        return VerificationResult.Valid;
    }
}
