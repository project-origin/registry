using ProjectOrigin.Register.LineProcessor.Models;

namespace ProjectOrigin.Register.LineProcessor.Interfaces;

public interface ICommandStepProcessor
{
    Task<CommandStepResult> Process(CommandStep request);
}
