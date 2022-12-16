using Google.Protobuf;
using ProjectOrigin.Electricity.Interfaces;
using ProjectOrigin.Register.StepProcessor.Interfaces;
using ProjectOrigin.Register.StepProcessor.Models;
using ProjectOrigin.Register.Utils.Interfaces;
using ProjectOrigin.Register.V1;

namespace ProjectOrigin.Electricity.Services;

internal class ElectricityStepVerifier : ICommandStepVerifiere
{
    private const string VerifyMethodName = nameof(IEventVerifier<object>.Verify);

    private IServiceProvider _serviceProvider;
    private IProtoSerializer _protoSerializer;
    private IModelHydrater _modelHydrater;

    public ElectricityStepVerifier(IServiceProvider serviceProvider, IProtoSerializer protoSerializer, IModelHydrater modelHydrater)
    {
        _serviceProvider = serviceProvider;
        _protoSerializer = protoSerializer;
        _modelHydrater = modelHydrater;
    }

    public Task<VerificationResult> Verify(CommandStep commandStep, IDictionary<FederatedStreamId, IEnumerable<SignedEvent>> streams)
    {
        var @event = _protoSerializer.Deserialize(commandStep.SignedEvent.Type, commandStep.SignedEvent.Payload);
        var requestType = typeof(Register.StepProcessor.Interfaces.VerificationRequest<>).MakeGenericType(@event.GetType());
        var modelDictionary = ToModelDictionary(streams);

        var verifierInterfaceType = modelDictionary.TryGetValue(commandStep.RoutingId, out var model)
            ? typeof(IEventVerifier<,>).MakeGenericType(model.GetType(), @event.GetType())
            : typeof(IEventVerifier<>).MakeGenericType(@event.GetType());
        var request = CreateVerificationRequest(commandStep, @event, requestType, modelDictionary);
        return ExecuteRequestOnVerifier(verifierInterfaceType, request);
    }

    private Task<VerificationResult> ExecuteRequestOnVerifier(Type verifierInterfaceType, object request)
    {
        var verifier = _serviceProvider.GetService(verifierInterfaceType);
        var methodInfo = verifierInterfaceType.GetMethod(VerifyMethodName) ?? throw new Exception($"Could not find ”{VerifyMethodName}” method");
        return methodInfo.Invoke(verifier, new object[] { request }) as Task<VerificationResult> ?? throw new Exception("Imposible exception, result is wrong type");
    }

    private IDictionary<FederatedStreamId, object> ToModelDictionary(IDictionary<FederatedStreamId, IEnumerable<SignedEvent>> streams)
    {
        return streams
            .Select(keyValue =>
            {
                var events = keyValue.Value.Select(x => _protoSerializer.Deserialize(x.Type, x.Payload));
                var model = _modelHydrater.HydrateModel(events);
                return (type: keyValue.Key, model);
            })
            .Where(x => x.model is not null)
            .ToDictionary(a => a.type, a => a.model!);
    }

    private static object CreateVerificationRequest(CommandStep commandStep, IMessage @event, Type requestType, IDictionary<FederatedStreamId, object> modelDictionary)
    {
        return Activator.CreateInstance(requestType, @event, commandStep.SignedEvent.Signature.ToByteArray(), modelDictionary) ?? throw new Exception($"VerificationRequest could was not created");
    }

}
