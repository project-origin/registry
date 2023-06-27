using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Protobuf;
using ProjectOrigin.Electricity.Server.Interfaces;

namespace ProjectOrigin.Electricity.Server.Services;

public class VerifierDispatcher : IVerifierDispatcher
{
    private const string VerifyMethodName = nameof(IEventVerifier<IMessage>.Verify);

    private IServiceProvider _serviceProvider;
    private IProtoDeserializer _protoDeserializer;
    private IModelHydrater _modelHydrater;

    public VerifierDispatcher(IServiceProvider serviceProvider, IProtoDeserializer protoDeserializer, IModelHydrater modelHydrater)
    {
        _serviceProvider = serviceProvider;
        _protoDeserializer = protoDeserializer;
        _modelHydrater = modelHydrater;
    }

    public Task<VerificationResult> Verify(Registry.V1.Transaction transaction, IEnumerable<Registry.V1.Transaction> stream)
    {
        var model = _modelHydrater.HydrateModel(stream.Select(e => _protoDeserializer.Deserialize(e.Header.PayloadType, e.Payload)));
        var @event = _protoDeserializer.Deserialize(transaction.Header.PayloadType, transaction.Payload);
        var verifierInterfaceType = typeof(IEventVerifier<>).MakeGenericType(@event.GetType());

        var verifier = _serviceProvider.GetService(verifierInterfaceType)
            ?? throw new Exception($"Verifier for ”{verifierInterfaceType}” could not be resolved");

        var methodInfo = verifier.GetType().GetMethod(VerifyMethodName)
            ?? throw new Exception($"Could not find ”{VerifyMethodName}” method");

        return methodInfo.Invoke(verifier, new object[] { transaction, model!, @event }) as Task<VerificationResult>
            ?? throw new Exception("Imposible exception, result is wrong type");
    }
}
