namespace Sl23.Reader.Tests;

public class FrameRecordTests
{
    private static FrameRecord Minimal(ushort channel = 0) => new()
    {
        Format = FileFormat.Sl2,
        Position = 8,
        OffsetField = 8,
        HeaderSize = 144,
        FrameSize = 200,
        EchoSize = 56,
        ChannelId = channel,
    };

    [Fact]
    public void AFieldTheFrameDoesNotCarryIsAbsent()
    {
        var record = Minimal();

        Assert.Null(record.FrameIndex);
        Assert.Null(record.PreviousFrameSize);
        Assert.Null(record.RangeLimitsMetres);
        Assert.Null(record.DepthMetres);
        Assert.Null(record.Flags);
        Assert.Null(record.Longitude);
        Assert.Null(record.Latitude);
        Assert.Null(record.GpsSpeedKnots);
        Assert.Null(record.CourseRadians);
        Assert.Null(record.TimeRaw);
        Assert.Null(record.TimeSigned);
        Assert.Null(record.PreviousFrameOffsets);
        Assert.Null(record.FrameVersion);
        Assert.Null(record.HeaderBytes);
        Assert.Null(record.Echo);
    }

    [Theory]
    [InlineData(2, "DownScan")]
    [InlineData(99, "Unknown(99)")]
    public void TheLabelFollowsTheChannelId(int id, string label)
    {
        Assert.Equal(label, Minimal((ushort)id).ChannelLabel);
    }

    [Fact]
    public void RangeLimitsAreConvertedToMetres()
    {
        var record = Minimal() with { UpperLimitFeet = 0f, LowerLimitFeet = 100f };

        Assert.NotNull(record.RangeLimitsMetres);
        Assert.Equal(30.48, record.RangeLimitsMetres.Value.Lower, 10);
    }

    [Fact]
    public void RangeLimitsAreAbsentWhenOneLimitIsMissing()
    {
        Assert.Null((Minimal() with { UpperLimitFeet = 0f }).RangeLimitsMetres);
        Assert.Null((Minimal() with { LowerLimitFeet = 100f }).RangeLimitsMetres);
    }

    [Fact]
    public void RangeLimitsAreAbsentWhenTheyAreNotAscending()
    {
        Assert.Null((Minimal() with { UpperLimitFeet = 100f, LowerLimitFeet = 0f }).RangeLimitsMetres);
    }

    [Fact]
    public void DepthIsConvertedToMetres()
    {
        var record = Minimal() with { DepthFeet = 10f };

        Assert.Equal(3.048, record.DepthMetres!.Value, 6);
    }

    [Fact]
    public void PositionIsPresentOnlyWithThePositionFlag()
    {
        var withFlag = Minimal() with { Flags = Units.PositionValidFlag, X = 1109463, Y = 8371575 };
        var otherFlags = Minimal() with { Flags = 0x0002 | 0x0004, X = 1109463, Y = 8371575 };
        var noFlags = Minimal() with { X = 1109463, Y = 8371575 };

        Assert.Equal(10.0000038, withFlag.Longitude!.Value, 6);
        Assert.Equal(59.9999993, withFlag.Latitude!.Value, 6);
        Assert.Null(otherFlags.Longitude);
        Assert.Null(otherFlags.Latitude);
        Assert.Null(noFlags.Longitude);
        Assert.Null(noFlags.Latitude);
    }

    [Theory]
    [InlineData(0x0010 | 0x0002 | 0x0004)]
    [InlineData(0x0010 | 0x0200 | 0x0100 | 0x0080 | 0x0040 | 0x0008)]
    [InlineData(0xFFFF)]
    public void PositionIsPresentWhenThePositionFlagIsSetAmongOtherFlags(int flags)
    {
        var record = Minimal() with { Flags = (ushort)flags, X = 1109463, Y = 8371575 };

        Assert.Equal(10.0000038, record.Longitude!.Value, 6);
        Assert.Equal(59.9999993, record.Latitude!.Value, 6);
    }

    [Fact]
    public void PositionIsAbsentWhenEveryFlagButThePositionFlagIsSet()
    {
        var record = Minimal() with { Flags = 0xFFFF & ~Units.PositionValidFlag, X = 1109463, Y = 8371575 };

        Assert.Null(record.Longitude);
        Assert.Null(record.Latitude);
    }

    [Fact]
    public void ALongitudeNeedsXAndALatitudeNeedsY()
    {
        var onlyY = Minimal() with { Flags = Units.PositionValidFlag, Y = 8371575 };

        Assert.Null(onlyY.Longitude);
        Assert.NotNull(onlyY.Latitude);
    }

    [Fact]
    public void ALatitudeNeedsYAndALongitudeNeedsX()
    {
        var onlyX = Minimal() with { Flags = Units.PositionValidFlag, X = 1109463 };

        Assert.NotNull(onlyX.Longitude);
        Assert.Null(onlyX.Latitude);
    }

    [Fact]
    public void TheSignedTimeIsDerivedFromTheRawTime()
    {
        var record = Minimal() with { TimeRaw = uint.MaxValue };

        Assert.Equal(uint.MaxValue, record.TimeRaw);
        Assert.Equal(-1, record.TimeSigned);
    }
}
