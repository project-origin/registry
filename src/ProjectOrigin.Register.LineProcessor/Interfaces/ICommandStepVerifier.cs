using Google.Protobuf;
using ProjectOrigin.Register.LineProcessor.Models;

namespace ProjectOrigin.Register.LineProcessor.Interfaces;

public interface ICommandStepVerifier<TEvent, TModel> where TEvent : IMessage where TModel : IModel
{
    Task<VerificationResult> Verify(CommandStep<TEvent> commandStep, TModel? model);
}
