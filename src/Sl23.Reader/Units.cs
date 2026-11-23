namespace Sl23.Reader;

/// <summary>The conversions of design section 6.8, and the flag bit the record needs.</summary>
public static class Units
{
    /// <summary>The flag bit that says x and y are meaningful (design section 6.7).</summary>
    public const ushort PositionValidFlag = 0x0010;

    /// <summary>Converts feet, the file's unit for depth and range limits, to metres.</summary>
    /// <param name="feet">The value in feet.</param>
    /// <returns>The value in metres (feet times 0.3048).</returns>
    public static double FeetToMetres(double feet) => throw new NotImplementedException();

    /// <summary>Converts a pair of range limits in feet to metres, or reports them absent.</summary>
    /// <param name="upperFeet">The upper limit, the top of the echo column.</param>
    /// <param name="lowerFeet">The lower limit, the bottom of the echo column.</param>
    /// <returns>The limits in metres, or <see langword="null"/> when either is not finite or the upper limit is not smaller than the lower (design section 7.1).</returns>
    public static (double Upper, double Lower)? RangeLimitsToMetres(float upperFeet, float lowerFeet) => throw new NotImplementedException();

    /// <summary>Converts the file's Mercator x to longitude on the design's sphere (R = 6,356,752.3142 m).</summary>
    /// <param name="x">The x value in Mercator metres.</param>
    /// <returns>The longitude in degrees.</returns>
    public static double ToLongitude(int x) => throw new NotImplementedException();

    /// <summary>Converts the file's Mercator y to latitude on the design's sphere (R = 6,356,752.3142 m).</summary>
    /// <param name="y">The y value in Mercator metres.</param>
    /// <returns>The latitude in degrees.</returns>
    public static double ToLatitude(int y) => throw new NotImplementedException();

    /// <summary>Reads the raw time field as a signed value, the second reading the inventory records (design section 6.8).</summary>
    /// <param name="raw">The u32 as stored.</param>
    /// <returns>The same 32 bits as a signed integer.</returns>
    public static int TimeAsSigned(uint raw) => throw new NotImplementedException();
}
