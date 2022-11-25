using Google.Protobuf;
using ProjectOrigin.Register.StepProcessor.Models;

namespace ProjectOrigin.Register.StepProcessor.Interfaces;

public interface ICommandStepVerifier<TEvent, TModel> where TEvent : IMessage where TModel : IModel
{
    Task<VerificationResult> Verify(CommandStep<TEvent> commandStep, TModel? model);
}
