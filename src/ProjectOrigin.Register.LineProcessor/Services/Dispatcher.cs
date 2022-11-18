using Google.Protobuf;
using ProjectOrigin.Register.LineProcessor.Interfaces;
using ProjectOrigin.Register.LineProcessor.Models;

namespace ProjectOrigin.Register.LineProcessor.Services;

public class Dispatcher : ICommandStepDispatcher
{
    private IModelLoader modelLoader;
    private ICommandStepVerifierFactory verifierFactory;
    private Dictionary<(Type modelType, Type eventType), Func<CommandStep, Task<(VerificationResult, int)>>> verifierDictionary;

    public Dispatcher(IModelLoader modelLoader, ICommandStepVerifierFactory verifierFactory)
    {
        this.modelLoader = modelLoader;
        this.verifierFactory = verifierFactory;
        verifierDictionary = verifierFactory.SupportedTypes.Select(v => GetVerifyFunction(v)).ToDictionary(res => res.typeKey, res => res.function);
    }

    public Task<(VerificationResult Result, int NextEventIndex)> Verify(CommandStep request)
    {
        var modelType = request.ModelType;
        var eventType = request.SignedEvent.Event.GetType();

        var verifier = verifierDictionary[(modelType, eventType)];

        return verifier(request);
    }

    private ((Type modelType, Type eventType) typeKey, Func<CommandStep, Task<(VerificationResult, int)>> function) GetVerifyFunction(Type verifierType)
    {
        Type genericInterfaceType = typeof(ICommandStepVerifier<,>);
        var interfaceType = verifierType.GetInterfaces().Single(i => i.GetGenericTypeDefinition() == genericInterfaceType);
        var argumentTypes = interfaceType.GetGenericArguments();

        var eventType = argumentTypes[0];
        var modelType = argumentTypes[1];

        var methodInfo = interfaceType.GetMethod(nameof(ICommandStepVerifier<IMessage, IModel>.Verify)) ?? throw new InvalidOperationException($"{interfaceType.Name} does not have a verify method");
        if (methodInfo.ReturnType != typeof(Task<VerificationResult>)) throw new InvalidOperationException("Verify does not return Task<VerificationResult>");

        var verifier = verifierFactory.Get(verifierType);

        var func = async (CommandStep request) =>
        {
            var (model, eventCount) = await modelLoader.Get(request.FederatedStreamId, modelType);

            var result = await (methodInfo.Invoke(verifier,
                new object[]{request, model!
                }) as Task<VerificationResult>)!;

            return (result, eventCount);
        };

        return ((modelType, eventType), func);
    }
}
