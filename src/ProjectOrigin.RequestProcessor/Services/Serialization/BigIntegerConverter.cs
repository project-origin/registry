using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProjectOrigin.RequestProcessor.Services.Serialization;

public class BigIntegerConverter : JsonConverter<BigInteger>
{
    public override BigInteger Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return new BigInteger(reader.GetBytesFromBase64());
    }
    public override void Write(Utf8JsonWriter writer, BigInteger value, JsonSerializerOptions options)
    {
        writer.WriteBase64StringValue(value.ToByteArray());
    }
}
