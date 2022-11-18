using ProjectOrigin.Register.LineProcessor.Models;

namespace ProjectOrigin.Register.LineProcessor.Interfaces;

public interface ICommandStepOrchestrator
{
    Task<CommandStepResult> Process(CommandStep request);
}
