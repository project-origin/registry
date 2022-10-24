using System.Text;
using System.Text.Json;
using ProjectOrigin.RequestProcessor.Interfaces;
using ProjectOrigin.RequestProcessor.Services.Serialization;
using ProjectOrigin.VerifiableEventStore.Models;

namespace ProjectOrigin.RequestProcessor.Services;

public class JsonEventSerializer : IEventSerializer
{
    private Lazy<JsonSerializerOptions> serializerOptions = new Lazy<JsonSerializerOptions>(() =>
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new BigIntegerConverter());
        return options;
    });

    public object Deserialize(Event e)
    {
        try
        {
            return Unwrap(e.Content);
        }
        catch (Exception ex)
        {
            throw new Exception($"Could not deserialize Event ”{e.Id}”", ex);
        }
    }

    public Event Serialize(EventId id, object e)
    {
        try
        {
            return new Event(id, Wrap(e));
        }
        catch (Exception ex)
        {
            throw new Exception($"Could not serialize Event ”{id}”", ex);
        }
    }

    public byte[] Serialize(object e)
    {
        var json = JsonSerializer.Serialize(e, serializerOptions.Value);
        return Encoding.UTF8.GetBytes(json);
    }

    private byte[] Wrap(object o)
    {
        var serializedObject = Serialize(o);
        var eventTypeName = o.GetType().FullName ?? throw new Exception($"No Fullname for type ”{o.GetType().ToString()}”");
        var internalEvent = new EventWrapper(eventTypeName, serializedObject);
        return Serialize(internalEvent);
    }

    private object Unwrap(byte[] bytes)
    {
        var wrapper = JsonSerializer.Deserialize<EventWrapper>(bytes, serializerOptions.Value) ?? throw new Exception($"Could deserialize to EventWrapper");

        //TODO optimize!
        foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (var t in a.GetTypes())
            {
                if (t.FullName == wrapper.EventType)
                {
                    var json = Encoding.UTF8.GetString(wrapper.EventBytes);
                    return JsonSerializer.Deserialize(wrapper.EventBytes, t, serializerOptions.Value) ?? throw new Exception($"Could not deserialize to type {t.Name}");
                }
            }
        }

        throw new Exception($"Type ”{wrapper.EventType}” could not be found.");
    }


    record EventWrapper(string EventType, byte[] EventBytes);
}
