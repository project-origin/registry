using ProjectOrigin.RequestProcessor.Interfaces;
using ProjectOrigin.RequestProcessor.Models;

namespace ProjectOrigin.RequestProcessor.Services;

public class Dispatcher : IDispatcher
{
    private IModelLoader modelLoader;
    private Dictionary<Type, Func<IPublishRequest, IModelLoader, Task<(VerificationResult, int)>>> verifierDictionary;

    public Dispatcher(IEnumerable<Type> verifiers, IModelLoader modelLoader)
    {
        verifierDictionary = verifiers.Select(v => GetVerifyFunction(v)).ToDictionary(res => res.requestType, res => res.function);
        this.modelLoader = modelLoader;
    }

    public Task<(VerificationResult Result, int NextEventIndex)> Verify(IPublishRequest request)
    {
        var requestType = request.GetType();

        var verifier = verifierDictionary[requestType];

        return verifier(request, modelLoader);
    }

    static readonly Type genericInterfaceType = typeof(IRequestVerifier<,>);

    private (Type requestType, Func<IPublishRequest, IModelLoader, Task<(VerificationResult, int)>> function) GetVerifyFunction(Type verifierType)
    {
        var interfaceType = verifierType.GetInterfaces().Single(i => i.GetGenericTypeDefinition() == genericInterfaceType);
        var argumentTypes = interfaceType.GetGenericArguments();

        var requestType = argumentTypes[0];
        var modelType = argumentTypes[1];

        var methodInfo = interfaceType.GetMethod(nameof(IRequestVerifier<PublishRequest, object>.Verify)) ?? throw new InvalidOperationException("IRequestVerifier does not have a verify method");
        if (methodInfo.ReturnType != typeof(Task<VerificationResult>)) throw new InvalidOperationException("Verify does not return Task");

        var verifier = Activator.CreateInstance(verifierType);

        var func = async (IPublishRequest request, IModelLoader ml) =>
        {
            var (model, eventCount) = await ml.Get(request.FederatedStreamId, modelType);

            var result = await (methodInfo.Invoke(verifier, new object[]{
                request, model!
            }) as Task<VerificationResult>)!;

            return (result, eventCount);
        };

        return (requestType, func);
    }
}
