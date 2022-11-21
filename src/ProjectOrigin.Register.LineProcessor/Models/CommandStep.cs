using System.Security.Cryptography;
using Google.Protobuf;
using ProtoBuf;

namespace ProjectOrigin.Register.LineProcessor.Models;

public record CommandStep<TEvent>(FederatedStreamId FederatedStreamId, SignedEvent<TEvent> SignedEvent, Type ModelType, IMessage? Proof = null) : CommandStep(FederatedStreamId, SignedEvent, ModelType, Proof) where TEvent : IMessage
{
    public new SignedEvent<TEvent> SignedEvent
    {
        get
        {
            return (SignedEvent<TEvent>)base.SignedEvent;
        }
    }
}

public abstract record CommandStep(
    FederatedStreamId FederatedStreamId,
    SignedEvent SignedEvent,
    Type ModelType,
    IMessage? Proof)
{
    public CommandStepId CommandStepId
    {
        get
        {
            using var ms = new MemoryStream();
            var obj = new SerializedCommandStep(FederatedStreamId, SignedEvent.Serialize(), ModelType.FullName!, Proof?.ToByteArray());

            Serializer.Serialize(ms, obj);
            var hash = SHA256.HashData(ms.ToArray());
            return new CommandStepId(hash);
        }
    }

    [ProtoContract]
    record SerializedCommandStep(
        [property: ProtoMember(1)] FederatedStreamId FederatedStreamId,
        [property: ProtoMember(2)] byte[] SignedEvent,
        [property: ProtoMember(3)] string ModelType,
        [property: ProtoMember(4)] byte[]? Proof);

}
