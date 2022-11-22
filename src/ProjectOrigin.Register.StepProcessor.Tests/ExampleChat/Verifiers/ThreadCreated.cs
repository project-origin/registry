using ProjectOrigin.Register.StepProcessor.Interfaces;
using ProjectOrigin.Register.StepProcessor.Models;

namespace ProjectOrigin.Register.StepProcessor.Tests.ExampleChat;

public class ChatCreatedVerifier : ICommandStepVerifier<ChatCreatedEvent, Chat>
{
    public Task<VerificationResult> Verify(CommandStep<ChatCreatedEvent> commandStep, Chat? model)
    {
        if (model != null)
            return new VerificationResult.Invalid("Invalid request, chat already exists.");

        return new VerificationResult.Valid();
    }
}
