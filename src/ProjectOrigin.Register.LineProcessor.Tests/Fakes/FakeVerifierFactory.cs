using ProjectOrigin.Register.LineProcessor.Interfaces;
using ProjectOrigin.Register.LineProcessor.Tests.ExampleChat;

namespace ProjectOrigin.Services.Tests;

public class FakeVerifierFactory : ICommandStepVerifierFactory
{
    public IEnumerable<Type> SupportedTypes => new List<Type>(){
        typeof(ChatCreatedVerifier),
        typeof(MessagePostedVerifier)
    };

    public object Get(Type type) => Activator.CreateInstance(type)!;
}
