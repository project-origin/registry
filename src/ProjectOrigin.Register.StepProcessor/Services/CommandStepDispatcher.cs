using Google.Protobuf;
using ProjectOrigin.Register.StepProcessor.Interfaces;
using ProjectOrigin.Register.StepProcessor.Models;

namespace ProjectOrigin.Register.StepProcessor.Services;

public class CommandStepDispatcher : ICommandStepDispatcher
{
    private IModelLoader _modelLoader;
    private ICommandStepVerifierFactory _verifierFactory;
    private Dictionary<(Type modelType, Type eventType), Func<CommandStep, Task<(VerificationResult, int)>>> _verifierDictionary;

    public CommandStepDispatcher(IModelLoader modelLoader, ICommandStepVerifierFactory verifierFactory)
    {
        _modelLoader = modelLoader;
        _verifierFactory = verifierFactory;
        _verifierDictionary = verifierFactory.SupportedTypes.Select(v => GetVerifyFunction(v)).ToDictionary(res => res.typeKey, res => res.function);
    }

    public Task<(VerificationResult Result, int NextEventIndex)> Verify(CommandStep request)
    {
        var modelType = request.ModelType;
        var eventType = request.SignedEvent.Event.GetType();

        var verifier = _verifierDictionary[(modelType, eventType)];

        return verifier(request);
    }

    private ((Type modelType, Type eventType) typeKey, Func<CommandStep, Task<(VerificationResult, int)>> function) GetVerifyFunction(Type verifierType)
    {
        var genericInterfaceType = typeof(ICommandStepVerifier<,>);
        var interfaceType = verifierType.GetInterfaces().Single(i => i.GetGenericTypeDefinition() == genericInterfaceType);
        var argumentTypes = interfaceType.GetGenericArguments();

        var eventType = argumentTypes[0];
        var modelType = argumentTypes[1];

        var methodInfo = interfaceType.GetMethod(nameof(ICommandStepVerifier<IMessage, IModel>.Verify)) ?? throw new InvalidOperationException($"{interfaceType.Name} does not have a verify method");
        if (methodInfo.ReturnType != typeof(Task<VerificationResult>)) throw new InvalidOperationException("Verify does not return Task<VerificationResult>");

        var verifier = _verifierFactory.Get(verifierType);

        var func = async (CommandStep request) =>
        {
            var (model, eventCount) = await _modelLoader.Get(request.FederatedStreamId, modelType);

            var result = await (methodInfo.Invoke(verifier,
                new object[]{request, model!
                }) as Task<VerificationResult>)!;

            return (result, eventCount);
        };

        return ((modelType, eventType), func);
    }
}
