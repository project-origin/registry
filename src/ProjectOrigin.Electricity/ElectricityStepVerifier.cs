using System.Reflection;
using ProjectOrigin.Register.StepProcessor.Interfaces;
using ProjectOrigin.Register.StepProcessor.Models;
using ProjectOrigin.Register.Utils.Services;
using ProjectOrigin.Register.V1;

namespace ProjectOrigin.Electricity;

public class ElectricityStepVerifier : ICommandStepVerifier
{
    private ProtoSerializer _protoSerializer;
    private ModelHydrater _modelHydrater;

    public ElectricityStepVerifier()
    {
        _protoSerializer = new ProtoSerializer(Assembly.GetExecutingAssembly());
        _modelHydrater = new ModelHydrater();
    }

    public Task<VerificationResult> Verify(CommandStep request, Dictionary<FederatedStreamId, IEnumerable<SignedEvent>> streams)
    {
        var stream = streams[request.RoutingId];
        var eventStream = stream.Select(x => _protoSerializer.Deserialize(x.Type, x.Payload));

        dynamic model = _modelHydrater.HydrateModel(eventStream);

        throw new NotImplementedException();

        //return model.Verify(request, streams);
    }
}
