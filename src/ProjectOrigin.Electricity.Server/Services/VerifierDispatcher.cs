using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using ProjectOrigin.Electricity.Server.Interfaces;

namespace ProjectOrigin.Electricity.Server.Services;

public class VerifierDispatcher : IVerifierDispatcher
{
    private const string VerifyMethodName = nameof(IEventVerifier<IMessage>.Verify);
    private readonly ILogger<VerifierDispatcher> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IProtoDeserializer _protoDeserializer;
    private readonly IModelHydrater _modelHydrater;

    public VerifierDispatcher(ILogger<VerifierDispatcher> logger, IServiceProvider serviceProvider, IProtoDeserializer protoDeserializer, IModelHydrater modelHydrater)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _protoDeserializer = protoDeserializer;
        _modelHydrater = modelHydrater;
    }

    public Task<VerificationResult> Verify(Registry.V1.Transaction transaction, IEnumerable<Registry.V1.Transaction> stream)
    {
        var model = _modelHydrater.HydrateModel(stream.Select(e => _protoDeserializer.Deserialize(e.Header.PayloadType, e.Payload)));
        var @event = _protoDeserializer.Deserialize(transaction.Header.PayloadType, transaction.Payload);
        var verifierInterfaceType = typeof(IEventVerifier<>).MakeGenericType(@event.GetType());

        var verifier = _serviceProvider.GetService(verifierInterfaceType);

        if (verifier is null)
        {
            _logger.LogError("Could not find verifier for type ”{payloadType}”", transaction.Header.PayloadType);
            return new VerificationResult.Invalid($"No verifier implemented for payload type ”{transaction.Header.PayloadType}”");
        }

        var methodInfo = verifier.GetType().GetMethod(VerifyMethodName) ??
            throw new NotImplementedException($"Could not find ”{VerifyMethodName}” method for type ”{transaction.Header.PayloadType}”");

        return methodInfo.Invoke(verifier, new object[] { transaction, model!, @event }) as Task<VerificationResult>
            ?? throw new Exception("Imposible exception, result is wrong type");
    }
}
