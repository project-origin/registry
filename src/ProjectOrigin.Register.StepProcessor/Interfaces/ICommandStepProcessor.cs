namespace ProjectOrigin.Register.StepProcessor.Interfaces;

public interface ICommandStepProcessor
{
    Task<V1.CommandStepStatus> Process(V1.CommandStep request);
}
