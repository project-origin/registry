using System;
using Xunit;
using ProjectOrigin.Registry.Extensions;
using FluentAssertions;

namespace ProjectOrigin.Registry.Tests;

public class DateTimeOffsetExtensionsTests
{
    [Fact]
    public void TruncateMicroSeconds_ShouldTruncateMicroSeconds()
    {
        var dateTimeOffset = new DateTimeOffset(2021, 1, 2, 3, 4, 5, 6, 123, TimeSpan.Zero);
        var expectedDateTimeOffset = new DateTimeOffset(2021, 1, 2, 3, 4, 5, 6, 0, TimeSpan.Zero);

        var result = dateTimeOffset.TruncateMicroSeconds();

        result.Should().Be(expectedDateTimeOffset);
    }
}
