using System.Text.Json.Serialization;

namespace ProjectOrigin.Registry.Server.Options;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CacheTypes { InMemory, Redis }
