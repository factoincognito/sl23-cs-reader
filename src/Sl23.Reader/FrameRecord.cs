namespace Sl23.Reader;

/// <summary>
/// One frame of an SL2 or SL3 file, as every analysis sees it (design section 7.1). A field the file does
/// not have, or that the frame's layout does not carry, is <see langword="null"/>: it is never guessed.
/// </summary>
public sealed record FrameRecord
{
    /// <summary>Gets the file format of the frame's file: SL2 or SL3.</summary>
    public required FileFormat Format { get; init; }

    /// <summary>Gets the frame's real position in the file, in bytes (64-bit).</summary>
    public required long Position { get; init; }

    /// <summary>Gets the frame's own "offset in the file" field as stored (u32), so that a mismatch with <see cref="Position"/> is visible.</summary>
    public required uint OffsetField { get; init; }

    /// <summary>Gets the header size in bytes.</summary>
    public required int HeaderSize { get; init; }

    /// <summary>Gets the frame size in bytes, header plus echo.</summary>
    public required int FrameSize { get; init; }

    /// <summary>Gets the echo size in bytes.</summary>
    public required int EchoSize { get; init; }

    /// <summary>Gets the channel id.</summary>
    public required ushort ChannelId { get; init; }

    /// <summary>Gets the channel's label (design section 6.6).</summary>
    public string ChannelLabel => ChannelLabels.For(ChannelId);

    /// <summary>Gets the frame index; absent on a frame with a 128-byte SL3 header.</summary>
    public uint? FrameIndex { get; init; }

    /// <summary>Gets the previous frame size as stored.</summary>
    public ushort? PreviousFrameSize { get; init; }

    /// <summary>Gets the upper range limit in feet, as stored.</summary>
    public float? UpperLimitFeet { get; init; }

    /// <summary>Gets the lower range limit in feet, as stored.</summary>
    public float? LowerLimitFeet { get; init; }

    /// <summary>Gets the range limits in metres; absent when a limit is missing, not finite, or the upper is not smaller than the lower.</summary>
    public (double Upper, double Lower)? RangeLimitsMetres =>
        UpperLimitFeet is { } upper && LowerLimitFeet is { } lower ? Units.RangeLimitsToMetres(upper, lower) : null;

    /// <summary>Gets the water depth in feet, as stored.</summary>
    public float? DepthFeet { get; init; }

    /// <summary>Gets the water depth in metres.</summary>
    public double? DepthMetres => DepthFeet is { } feet ? Units.FeetToMetres(feet) : null;

    /// <summary>Gets the raw flags (u16).</summary>
    public ushort? Flags { get; init; }

    /// <summary>Gets the raw x value, Mercator metres.</summary>
    public int? X { get; init; }

    /// <summary>Gets the raw y value, Mercator metres.</summary>
    public int? Y { get; init; }

    private bool HasPosition => Flags is { } flags && (flags & Units.PositionValidFlag) != 0;

    /// <summary>Gets the longitude in degrees; present only when the position flag is set and x is present.</summary>
    public double? Longitude => HasPosition && X is { } x ? Units.ToLongitude(x) : null;

    /// <summary>Gets the latitude in degrees; present only when the position flag is set and y is present.</summary>
    public double? Latitude => HasPosition && Y is { } y ? Units.ToLatitude(y) : null;

    /// <summary>Gets the GPS speed in knots.</summary>
    public float? GpsSpeedKnots { get; init; }

    /// <summary>Gets the course over ground in radians.</summary>
    public float? CourseRadians { get; init; }

    /// <summary>Gets the raw time field (u32) as stored.</summary>
    public uint? TimeRaw { get; init; }

    /// <summary>Gets the time field read as a signed value, derived from <see cref="TimeRaw"/>.</summary>
    public int? TimeSigned => TimeRaw is { } raw ? Units.TimeAsSigned(raw) : null;

    /// <summary>Gets the six "previous frame offset" values, for check 7; only on SL3 frames with a 168 or 184 byte header.</summary>
    public IReadOnlyList<uint>? PreviousFrameOffsets { get; init; }

    /// <summary>Gets the frame version; SL3 only.</summary>
    public uint? FrameVersion { get; init; }

    /// <summary>Gets the raw header bytes; present only when an analysis asks for them.</summary>
    public ReadOnlyMemory<byte>? HeaderBytes { get; init; }

    /// <summary>Gets the echo bytes; present only when an analysis asks for them.</summary>
    public ReadOnlyMemory<byte>? Echo { get; init; }
}
