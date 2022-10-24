using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ProjectOrigin.RequestProcessor.Services.Serialization;

namespace ProjectOrigin.RequestProcessor.Models;

public abstract record PublishRequest<T>(FederatedStreamId FederatedStreamId, byte[] Signature, T Event) : PublishRequest(FederatedStreamId, Signature, Event) where T : class
{
    public new T Event
    {
        get
        {
            return (T)base.Event;
        }
        init
        {
            base.Event = value ?? throw new Exception("Must be set to an instance of an object");
        }
    }
}

public abstract record PublishRequest(FederatedStreamId FederatedStreamId, byte[] Signature, object Event) : IPublishRequest
{
    public RequestId RequestId
    {
        get
        {
            var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
            options.Converters.Add(new BigIntegerConverter());
            var json = JsonSerializer.Serialize(new { FederatedStreamId, Signature, Event }, options);
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(json));
            return new RequestId(hash);
        }
    }
}

public interface IPublishRequest
{
    object Event { get; }
    FederatedStreamId FederatedStreamId { get; }
    RequestId RequestId { get; }
}
