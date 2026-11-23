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

    /// <summary>Initializes a new instance of the <see cref="Sl2FrameReader"/> class; reading starts at file byte 8.</summary>
    /// <param name="stream">The file; it must be readable and seekable. The file header is not read here: the caller has found it to be SL2.</param>
    /// <param name="includeHeaderBytes">Whether each record carries the raw header bytes (for the survey).</param>
    /// <param name="includeEcho">Whether each record carries the echo bytes; otherwise they are skipped.</param>
    public Sl2FrameReader(Stream stream, bool includeHeaderBytes = false, bool includeEcho = false) => throw new NotImplementedException();

    /// <summary>Gets where the chain broke, once <see cref="ReadNext"/> has returned <see langword="null"/>; <see langword="null"/> when the chain ended exactly at the end of the file (check 4) or has not ended.</summary>
    public ChainBreak? Break => throw new NotImplementedException();

    /// <summary>Reads the next frame.</summary>
    /// <returns>The frame, or <see langword="null"/> at the end of the chain: at the end of the file, or where the chain broke (see <see cref="Break"/>).</returns>
    public FrameRecord? ReadNext() => throw new NotImplementedException();
}
