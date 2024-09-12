using System.Text.Json.Serialization;

namespace ProjectOrigin.Registry.Options;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CacheTypes { InMemory, Redis }
