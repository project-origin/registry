using System;

namespace ProjectOrigin.Registry.Extensions;

public static class DateTimeOffsetExtensions
{
    public static DateTimeOffset TruncateMicroSeconds(this DateTimeOffset dateTimeOffset)
        => new DateTimeOffset(
            dateTimeOffset.Year,
            dateTimeOffset.Month,
            dateTimeOffset.Day,
            dateTimeOffset.Hour,
            dateTimeOffset.Minute,
            dateTimeOffset.Second,
            dateTimeOffset.Millisecond,
            dateTimeOffset.Offset);
}
