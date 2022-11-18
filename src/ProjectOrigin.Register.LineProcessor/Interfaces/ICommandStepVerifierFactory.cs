namespace ProjectOrigin.Register.LineProcessor.Interfaces;

public interface ICommandStepVerifierFactory
{
    object Get(Type type);
    IEnumerable<Type> SupportedTypes { get; }
}
