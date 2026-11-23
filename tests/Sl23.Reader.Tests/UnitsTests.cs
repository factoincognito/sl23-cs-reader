namespace Sl23.Reader.Tests;

public class UnitsTests
{
    [Fact]
    public void ConvertsFeetToMetres()
    {
        Assert.Equal(30.48, Units.FeetToMetres(100), 10);
        Assert.Equal(0, Units.FeetToMetres(0), 10);
        Assert.Equal(-0.3048, Units.FeetToMetres(-1), 10);
    }

    [Fact]
    public void ConvertsRangeLimitsToMetres()
    {
        var limits = Units.RangeLimitsToMetres(0f, 100f);

        Assert.NotNull(limits);
        Assert.Equal(0, limits.Value.Upper, 10);
        Assert.Equal(30.48, limits.Value.Lower, 10);
    }

    [Fact]
    public void SidescanLimitsSymmetricAboutZeroAreValid()
    {
        var limits = Units.RangeLimitsToMetres(-50f, 50f);

        Assert.NotNull(limits);
        Assert.Equal(-15.24, limits.Value.Upper, 10);
        Assert.Equal(15.24, limits.Value.Lower, 10);
    }

    [Theory]
    [InlineData(10f, 10f)]
    [InlineData(100f, 0f)]
    [InlineData(float.NaN, 100f)]
    [InlineData(0f, float.NaN)]
    [InlineData(float.NegativeInfinity, 100f)]
    [InlineData(0f, float.PositiveInfinity)]
    public void RangeLimitsThatAreNotFiniteOrNotAscendingAreAbsent(float upper, float lower)
    {
        Assert.Null(Units.RangeLimitsToMetres(upper, lower));
    }

    [Fact]
    public void ConvertsMercatorXToLongitude()
    {
        Assert.Equal(0, Units.ToLongitude(0), 10);
        Assert.Equal(10.0000038, Units.ToLongitude(1109463), 6);
        Assert.Equal(-Units.ToLongitude(1109463), Units.ToLongitude(-1109463), 10);
    }

    [Fact]
    public void ConvertsMercatorYToLatitude()
    {
        Assert.Equal(0, Units.ToLatitude(0), 10);
        Assert.Equal(59.9999993, Units.ToLatitude(8371575), 6);
        Assert.Equal(-Units.ToLatitude(8371575), Units.ToLatitude(-8371575), 10);
    }

    [Theory]
    [InlineData(0u, 0)]
    [InlineData(5u, 5)]
    [InlineData(0x7FFFFFFFu, int.MaxValue)]
    [InlineData(0x80000000u, int.MinValue)]
    [InlineData(uint.MaxValue, -1)]
    public void ReadsTheSameBitsAsSigned(uint raw, int expected)
    {
        Assert.Equal(expected, Units.TimeAsSigned(raw));
    }

    [Fact]
    public void ThePositionFlagIsBit0x0010()
    {
        Assert.Equal((ushort)0x0010, Units.PositionValidFlag);
    }
}
