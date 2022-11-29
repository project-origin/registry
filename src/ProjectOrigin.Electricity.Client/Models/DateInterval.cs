using Google.Protobuf.WellKnownTypes;

namespace ProjectOrigin.Electricity.Client.Models;

/// <summary>
/// The span of time between a specific start date and end date.
/// </summary>
public class DateInterval
{
    /// <summary>
    /// The start date.
    /// </summary>
    public DateTimeOffset Start { get; }

    /// <summary>
    /// The end date.
    /// </summary>
    public DateTimeOffset End { get; }

    /// <summary>
    /// The duration.
    /// </summary>
    public TimeSpan Duration
    {
        get
        {
            return End - Start;
        }
    }

    /// <summary>
    /// Initializes an interval with the specified start and end date.
    /// </summary>
    /// <param name="start">The start date.</param>
    /// <param name="end">The end date.</param>
    public DateInterval(DateTimeOffset start, DateTimeOffset end)
    {
        if (start > end) throw new InvalidDataException("Start must be before end");

        Start = start;
        End = end;
    }

    /// <summary>
    /// Initializes an interval with the specified start date and duration.
    /// </summary>
    /// <param name="start">The start date.</param>
    /// <param name="duration">The length of the interval from the start date.</param>
    public DateInterval(DateTimeOffset start, TimeSpan duration) : this(start, start + duration)
    {
    }

    internal V1.DateInterval ToProto()
    {
        return new V1.DateInterval()
        {
            Start = Timestamp.FromDateTimeOffset(Start),
            End = Timestamp.FromDateTimeOffset(End),
        };
    }
}
