using System;
using System.ComponentModel.DataAnnotations;

namespace ProjectOrigin.VerifiableEventStore.Models;

public class BlockFinalizationOptions
{
    private TimeSpan _interval;

    [Required, Range(typeof(TimeSpan), "00:00:01", "1:00:00")]
    public TimeSpan Interval
    {
        get => _interval;
        set => _interval = value;
    }
}
