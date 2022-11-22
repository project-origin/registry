namespace ProjectOrigin.Register.StepProcessor.Interfaces;

public interface ICommandStepVerifierFactory
{
    object Get(Type type);
    IEnumerable<Type> SupportedTypes { get; }
}
