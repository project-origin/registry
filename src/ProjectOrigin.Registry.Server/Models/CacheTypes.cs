using System.Text.Json.Serialization;

namespace ProjectOrigin.Registry.Server.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CacheTypes { InMemory, Redis }
