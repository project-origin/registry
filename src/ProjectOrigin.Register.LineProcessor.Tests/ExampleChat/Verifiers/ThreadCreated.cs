using ProjectOrigin.Register.LineProcessor.Interfaces;
using ProjectOrigin.Register.LineProcessor.Models;

namespace ProjectOrigin.Register.LineProcessor.Tests.ExampleChat;

public class ChatCreatedVerifier : ICommandStepVerifier<ChatCreatedEvent, Chat>
{
    public Task<VerificationResult> Verify(CommandStep<ChatCreatedEvent> commandStep, Chat? model)
    {
        if (model != null)
            return new VerificationResult.Invalid("Invalid request, chat already exists.");

        return new VerificationResult.Valid();
    }
}
