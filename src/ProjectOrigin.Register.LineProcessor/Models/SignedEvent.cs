using System.Reflection;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using NSec.Cryptography;
using ProtoBuf;

namespace ProjectOrigin.Register.LineProcessor.Models;

public record SignedEvent<T>(T Event, byte[] Signature) : SignedEvent(Event, Signature) where T : IMessage
{
    public new T Event
    {
        get
        {
            return (T)base.Event;
        }
    }
}

// todo: move serializer to seperate interface and use factory to construct with allowed types.
public record SignedEvent(IMessage Event, byte[] Signature)
{
    public bool VerifySignature(PublicKey owner) => Ed25519.Ed25519.Verify(owner, Event.ToByteArray(), Signature);

    private static Lazy<Dictionary<string, (Type type, MessageDescriptor descriptor)>> lazyTypeDictionary = new Lazy<Dictionary<string, (Type type, MessageDescriptor descriptor)>>(() =>
    {
        var dictionary = new Dictionary<string, (Type type, MessageDescriptor descriptor)>();

        var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies().Where(x => x.FullName!.StartsWith("ProjectOrigin")).ToList();

        foreach (var assembly in loadedAssemblies)
        {
            foreach (var type in assembly.GetTypes())
            {
                if (type.IsClass && typeof(IMessage).IsAssignableFrom(type))
                {
                    var descriptor = (MessageDescriptor)type.GetProperty(nameof(IMessage.Descriptor), BindingFlags.Public | BindingFlags.Static)!.GetValue(null)!;
                    dictionary[type.FullName!] = (type, descriptor);
                }
            }
        }

        return dictionary;
    }, true);

    public byte[] Serialize()
    {
        using var ms = new MemoryStream();
        var content = new SerializedSignedEvent(Event.GetType().FullName!, Event.ToByteArray(), Signature);
        Serializer.Serialize(ms, content);
        return ms.ToArray();
    }

    public static SignedEvent Deserialize(byte[] bytes)
    {
        var serializedSignedEvent = Serializer.Deserialize<SerializedSignedEvent>(new ReadOnlySpan<byte>(bytes));

        var (eventType, descriptor) = lazyTypeDictionary.Value[serializedSignedEvent.Type];
        var obj = descriptor.Parser.ParseFrom(serializedSignedEvent.Event);

        var genericSignedEventType = typeof(SignedEvent<>).MakeGenericType(eventType);

        return Activator.CreateInstance(genericSignedEventType, obj, serializedSignedEvent.Signature) as SignedEvent ?? throw new Exception();
    }

    [ProtoContract(SkipConstructor = true)]
    record SerializedSignedEvent(
        [property: ProtoMember(1)] string Type,
        [property: ProtoMember(2)] byte[] Event,
        [property: ProtoMember(3)] byte[] Signature);
}

