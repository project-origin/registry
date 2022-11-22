using ProjectOrigin.Register.StepProcessor.Models;

namespace ProjectOrigin.Register.StepProcessor.Interfaces;

public interface ICommandStepProcessor
{
    Task<CommandStepResult> Process(CommandStep request);
}
