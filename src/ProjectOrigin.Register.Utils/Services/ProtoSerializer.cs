using System.Reflection;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using ProjectOrigin.Register.Utils.Interfaces;

namespace ProjectOrigin.Register.Utils.Services;

public class ProtoSerializer : IProtoSerializer
{
    private Dictionary<string, MessageDescriptor> _typeDictionary;

    public ProtoSerializer(Assembly assembly)
    {
        _typeDictionary = assembly.GetTypes()
            .Where(type =>
                type.IsClass
                && typeof(IMessage).IsAssignableFrom(type))
            .Select(type =>
            {
                var descriptor = (MessageDescriptor)type.GetProperty(nameof(IMessage.Descriptor), BindingFlags.Public | BindingFlags.Static)!.GetValue(null)!;
                return descriptor;
            })
            .ToDictionary(descriptor => descriptor.FullName, descriptor => descriptor);
    }

    public IMessage Deserialize(string type, ByteString content)
    {
        var descriptor = _typeDictionary[type];
        return descriptor.Parser.ParseFrom(content);
    }
}
