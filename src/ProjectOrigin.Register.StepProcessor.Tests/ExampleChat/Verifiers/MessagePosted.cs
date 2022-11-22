using ProjectOrigin.Register.StepProcessor.Interfaces;
using ProjectOrigin.Register.StepProcessor.Models;

namespace ProjectOrigin.Register.StepProcessor.Tests.ExampleChat;

public class MessagePostedVerifier : ICommandStepVerifier<MessagePostedEvent, Chat>
{
    public Task<VerificationResult> Verify(CommandStep<MessagePostedEvent> commandStep, Chat? model)
    {
        if (model == null)
            return new VerificationResult.Invalid("Invalid request, chat must exist to post message.");

        return new VerificationResult.Valid();
    }
}
