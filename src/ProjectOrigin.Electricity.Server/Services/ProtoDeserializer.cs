using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using ProjectOrigin.Electricity.Server.Exceptions;
using ProjectOrigin.Electricity.Server.Interfaces;

namespace ProjectOrigin.Electricity.Server.Services;

public class ProtoDeserializer : IProtoDeserializer
{
    private Dictionary<string, MessageDescriptor> _typeDictionary;

    public ProtoDeserializer(Assembly assembly)
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
        if (_typeDictionary.TryGetValue(type, out var descriptor))
        {
            try
            {
                return descriptor.Parser.ParseFrom(content);
            }
            catch (Exception ex)
            {
                throw new InvalidPayloadException($"Could not deserialize invalid payload of type ”{type}”", ex);
            }
        }

        throw new InvalidPayloadException($"Could not deserialize unknown type ”{type}”");
    }
}
