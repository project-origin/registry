using ProjectOrigin.RequestProcessor.Interfaces;
using ProjectOrigin.RequestProcessor.Models;

namespace ProjectOrigin.RequestProcessor.Tests.ExampleChat;

public record MessagePostedEvent(Guid topic, string message);

public record MessagePostedRequest(FederatedStreamId id, MessagePostedEvent e) : PublishRequest<MessagePostedEvent>(id, new byte[0], e);

public class MessagePostedVerifier : IRequestVerifier<MessagePostedRequest, Chat>
{
    public Task<VerificationResult> Verify(MessagePostedRequest request, Chat? model)
    {
        if (model == null)
            return VerificationResult.Invalid("Invalid request, chat must exist to post message.");

        return VerificationResult.Valid;
    }
}
