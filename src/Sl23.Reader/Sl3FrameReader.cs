using System.Buffers.Binary;

namespace Sl23.Reader;

/// <summary>
/// Reads the frames of an SL3 file one at a time (design sections 6.3 and 6.5). The header size is not fixed: it is the
/// frame size minus the echo size. The reader never holds more than one frame; the echo bytes are skipped unless asked for.
/// </summary>
public sealed class Sl3FrameReader
{
    /// <summary>The header size of the frames on channels 7 and 8; the smallest header (check 2).</summary>
    public const int SmallestHeaderSize = 128;

    /// <summary>The fewest bytes from which a frame can be read at all: the echo size ends at offset 48 (design section 6.3).</summary>
    public const int MinimumReadable = 48;

    private const int LargestKnownHeaderSize = 184;

    private readonly Stream stream;
    private readonly bool includeHeaderBytes;
    private readonly bool includeEcho;
    private readonly long length;
    private readonly byte[] buffer = new byte[LargestKnownHeaderSize];
    private long position = Sl23.Reader.FileHeader.Size;
    private bool finished;

    /// <summary>Initializes a new instance of the <see cref="Sl3FrameReader"/> class; reading starts at file byte 8.</summary>
    /// <param name="stream">The file; it must be readable and seekable. The file header is not read here: the caller has found it to be SL3.</param>
    /// <param name="includeHeaderBytes">Whether each record carries the raw header bytes (for the survey).</param>
    /// <param name="includeEcho">Whether each record carries the echo bytes; otherwise they are skipped.</param>
    public Sl3FrameReader(Stream stream, bool includeHeaderBytes = false, bool includeEcho = false)
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
        stream.ReadExactly(buffer, 0, (int)Math.Min(left, LargestKnownHeaderSize));

        var offsetField = BinaryPrimitives.ReadUInt32LittleEndian(buffer);
        if (offsetField != unchecked((uint)position))
        {
            return Stop(left, ChainBreakReason.OffsetMismatch);
        }

        int frameSize = BinaryPrimitives.ReadUInt16LittleEndian(buffer.AsSpan(8));
        if (frameSize < SmallestHeaderSize || frameSize > left)
        {
            return Stop(left, ChainBreakReason.InvalidFrameSize);
        }

        long echoSize = BinaryPrimitives.ReadUInt32LittleEndian(buffer.AsSpan(44));
        var headerSize = frameSize - echoSize;
        var carried = HeaderBytesOf(headerSize, frameSize);
        var echo = EchoOf(echoSize, frameSize);
        var record = headerSize is 168 or 184
            ? FullRecord(offsetField, frameSize, echoSize, headerSize)
            : new FrameRecord
            {
                Format = FileFormat.Sl3,
                Position = position,
                OffsetField = offsetField,
                HeaderSize = headerSize,
                FrameSize = frameSize,
                EchoSize = echoSize,
                ChannelId = BinaryPrimitives.ReadUInt16LittleEndian(buffer.AsSpan(12)),
            };

        position += frameSize;
        return record with { HeaderBytes = carried, Echo = echo };
    }

    private FrameRecord FullRecord(uint offsetField, int frameSize, long echoSize, long headerSize)
    {
        var previousOffsets = new uint[6];
        for (var i = 0; i < previousOffsets.Length; i++)
        {
            previousOffsets[i] = BinaryPrimitives.ReadUInt32LittleEndian(buffer.AsSpan(128 + (4 * i)));
        }

        return new FrameRecord
        {
            Format = FileFormat.Sl3,
            Position = position,
            OffsetField = offsetField,
            HeaderSize = headerSize,
            FrameSize = frameSize,
            EchoSize = echoSize,
            ChannelId = BinaryPrimitives.ReadUInt16LittleEndian(buffer.AsSpan(12)),
            FrameVersion = BinaryPrimitives.ReadUInt32LittleEndian(buffer.AsSpan(4)),
            PreviousFrameSize = BinaryPrimitives.ReadUInt16LittleEndian(buffer.AsSpan(10)),
            FrameIndex = BinaryPrimitives.ReadUInt32LittleEndian(buffer.AsSpan(16)),
            UpperLimitFeet = BinaryPrimitives.ReadSingleLittleEndian(buffer.AsSpan(20)),
            LowerLimitFeet = BinaryPrimitives.ReadSingleLittleEndian(buffer.AsSpan(24)),
            DepthFeet = BinaryPrimitives.ReadSingleLittleEndian(buffer.AsSpan(48)),
            GpsSpeedKnots = BinaryPrimitives.ReadSingleLittleEndian(buffer.AsSpan(84)),
            X = BinaryPrimitives.ReadInt32LittleEndian(buffer.AsSpan(92)),
            Y = BinaryPrimitives.ReadInt32LittleEndian(buffer.AsSpan(96)),
            CourseRadians = BinaryPrimitives.ReadSingleLittleEndian(buffer.AsSpan(104)),
            Flags = BinaryPrimitives.ReadUInt16LittleEndian(buffer.AsSpan(116)),
            TimeRaw = BinaryPrimitives.ReadUInt32LittleEndian(buffer.AsSpan(124)),
            PreviousFrameOffsets = previousOffsets,
        };
    }

    private ReadOnlyMemory<byte>? HeaderBytesOf(long headerSize, int frameSize)
    {
        if (!includeHeaderBytes)
        {
            return null;
        }

        var count = (int)Math.Clamp(headerSize, 0, frameSize);
        var bytes = new byte[count];
        stream.Seek(position, SeekOrigin.Begin);
        stream.ReadExactly(bytes);
        return bytes;
    }

    private ReadOnlyMemory<byte>? EchoOf(long echoSize, int frameSize)
    {
        if (!includeEcho)
        {
            return null;
        }

        var count = echoSize <= frameSize ? (int)echoSize : 0;
        var bytes = new byte[count];
        stream.Seek(position + frameSize - count, SeekOrigin.Begin);
        stream.ReadExactly(bytes);
        return bytes;
    }

    private FrameRecord? Stop(long bytesLeft, ChainBreakReason reason)
    {
        Break = new ChainBreak(position, bytesLeft, reason);
        finished = true;
        return null;
    }
}
