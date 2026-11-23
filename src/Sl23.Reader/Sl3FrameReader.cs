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

    /// <summary>Initializes a new instance of the <see cref="Sl3FrameReader"/> class; reading starts at file byte 8.</summary>
    /// <param name="stream">The file; it must be readable and seekable. The file header is not read here: the caller has found it to be SL3.</param>
    /// <param name="includeHeaderBytes">Whether each record carries the raw header bytes (for the survey).</param>
    /// <param name="includeEcho">Whether each record carries the echo bytes; otherwise they are skipped.</param>
    public Sl3FrameReader(Stream stream, bool includeHeaderBytes = false, bool includeEcho = false) => throw new NotImplementedException();

    /// <summary>Gets where the chain broke, once <see cref="ReadNext"/> has returned <see langword="null"/>; <see langword="null"/> when the chain ended exactly at the end of the file (check 4) or has not ended.</summary>
    public ChainBreak? Break => throw new NotImplementedException();

    /// <summary>Reads the next frame.</summary>
    /// <returns>The frame, or <see langword="null"/> at the end of the chain: at the end of the file, or where the chain broke (see <see cref="Break"/>).</returns>
    public FrameRecord? ReadNext() => throw new NotImplementedException();
}
