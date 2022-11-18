using ProjectOrigin.Register.LineProcessor.Interfaces;
using ProjectOrigin.Register.LineProcessor.Models;

namespace ProjectOrigin.Register.LineProcessor.Tests.ExampleChat;

public class MessagePostedVerifier : ICommandStepVerifier<MessagePostedEvent, Chat>
{
    public Task<VerificationResult> Verify(CommandStep<MessagePostedEvent> commandStep, Chat? model)
    {
        if (model == null)
            return new VerificationResult.Invalid("Invalid request, chat must exist to post message.");

        return new VerificationResult.Valid();
    }
}
