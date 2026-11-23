using System.Buffers.Binary;

namespace Sl23.Reader;

/// <summary>
/// Reads the frames of an SL2 file one at a time (design sections 6.3 and 6.4). The reader never holds more than
/// one frame; the echo bytes are skipped unless asked for.
/// </summary>
public sealed class Sl2FrameReader
{
    /// <summary>The size of an SL2 frame header in bytes; the echo starts at this offset in the frame.</summary>
    public const int HeaderSize = 144;

    /// <summary>The fewest bytes from which a frame can be read at all: the frame index ends at offset 40.</summary>
    public const int MinimumReadable = 36;

    private readonly Stream stream;
    private readonly bool includeHeaderBytes;
    private readonly bool includeEcho;
    private readonly long length;
    private readonly byte[] header = new byte[HeaderSize];
    private long position = Sl23.Reader.FileHeader.Size;
    private bool finished;

    /// <summary>Initializes a new instance of the <see cref="Sl2FrameReader"/> class; reading starts at file byte 8.</summary>
    /// <param name="stream">The file; it must be readable and seekable. The file header is not read here: the caller has found it to be SL2.</param>
    /// <param name="includeHeaderBytes">Whether each record carries the raw header bytes (for the survey).</param>
    /// <param name="includeEcho">Whether each record carries the echo bytes; otherwise they are skipped.</param>
    public Sl2FrameReader(Stream stream, bool includeHeaderBytes = false, bool includeEcho = false)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (!stream.CanRead || !stream.CanSeek)
        {
            throw new ArgumentException("The stream must be readable and seekable.", nameof(stream));
        }

        this.stream = stream;
        this.includeHeaderBytes = includeHeaderBytes;
        this.includeEcho = includeEcho;
        length = stream.Length;
        finished = length <= position;
    }

    /// <summary>Gets where the chain broke, once <see cref="ReadNext"/> has returned <see langword="null"/>; <see langword="null"/> when the chain ended exactly at the end of the file (check 4) or has not ended.</summary>
    public ChainBreak? Break { get; private set; }

    /// <summary>Reads the next frame.</summary>
    /// <returns>The frame, or <see langword="null"/> at the end of the chain: at the end of the file, or where the chain broke (see <see cref="Break"/>).</returns>
    public FrameRecord? ReadNext()
    {
        if (finished)
        {
            return null;
        }

        var left = length - position;
        if (left == 0)
        {
            finished = true;
            return null;
        }

        if (left < MinimumReadable)
        {
            return Stop(left, ChainBreakReason.TruncatedTail);
        }

        stream.Seek(position, SeekOrigin.Begin);
        stream.ReadExactly(header, 0, (int)Math.Min(left, HeaderSize));

        var offsetField = BinaryPrimitives.ReadUInt32LittleEndian(header);
        if (offsetField != unchecked((uint)position))
        {
            return Stop(left, ChainBreakReason.OffsetMismatch);
        }

        int frameSize = BinaryPrimitives.ReadUInt16LittleEndian(header.AsSpan(28));
        if (frameSize < HeaderSize || frameSize > left)
        {
            return Stop(left, ChainBreakReason.InvalidFrameSize);
        }

        var echoSize = BinaryPrimitives.ReadUInt16LittleEndian(header.AsSpan(34));
        var record = new FrameRecord
        {
            Format = FileFormat.Sl2,
            Position = position,
            OffsetField = offsetField,
            HeaderSize = frameSize - echoSize,
            FrameSize = frameSize,
            EchoSize = echoSize,
            ChannelId = BinaryPrimitives.ReadUInt16LittleEndian(header.AsSpan(32)),
            FrameIndex = BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(36)),
            PreviousFrameSize = BinaryPrimitives.ReadUInt16LittleEndian(header.AsSpan(30)),
            UpperLimitFeet = BinaryPrimitives.ReadSingleLittleEndian(header.AsSpan(40)),
            LowerLimitFeet = BinaryPrimitives.ReadSingleLittleEndian(header.AsSpan(44)),
            DepthFeet = BinaryPrimitives.ReadSingleLittleEndian(header.AsSpan(64)),
            GpsSpeedKnots = BinaryPrimitives.ReadSingleLittleEndian(header.AsSpan(100)),
            X = BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(108)),
            Y = BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(112)),
            CourseRadians = BinaryPrimitives.ReadSingleLittleEndian(header.AsSpan(120)),
            Flags = BinaryPrimitives.ReadUInt16LittleEndian(header.AsSpan(132)),
            TimeRaw = BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(140)),
            HeaderBytes = includeHeaderBytes ? new ReadOnlyMemory<byte>(header.ToArray()) : (ReadOnlyMemory<byte>?)null,
            Echo = includeEcho ? new ReadOnlyMemory<byte>(ReadEcho(frameSize - HeaderSize)) : (ReadOnlyMemory<byte>?)null,
        };

        position += frameSize;
        return record;
    }

    private byte[] ReadEcho(int count)
    {
        var echo = new byte[count];
        stream.ReadExactly(echo);
        return echo;
    }

    private FrameRecord? Stop(long bytesLeft, ChainBreakReason reason)
    {
        Break = new ChainBreak(position, bytesLeft, reason);
        finished = true;
        return null;
    }
}
