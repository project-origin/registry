using Google.Protobuf.WellKnownTypes;

namespace ProjectOrigin.Electricity.Shared;

public record TimePeriod(
    DateTimeOffset DateTimeFrom,
    DateTimeOffset DateTimeTo)
{
    public static implicit operator TimePeriod(V1.TimePeriod proto)
    {
        return new TimePeriod(
            proto.DateTimeFrom.ToDateTimeOffset(),
            proto.DateTimeTo.ToDateTimeOffset());
    }

    public static implicit operator V1.TimePeriod(TimePeriod obj)
    {
        return new V1.TimePeriod()
        {
            DateTimeFrom = Timestamp.FromDateTimeOffset(obj.DateTimeFrom),
            DateTimeTo = Timestamp.FromDateTimeOffset(obj.DateTimeTo),
        };
    }
}
