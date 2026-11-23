namespace Sl23.Reader;

/// <summary>Why the chain of frames broke (design section 6.3).</summary>
public enum ChainBreakReason
{
    /// <summary>Fewer bytes are left than a frame needs to be read at all (SL2: 36; SL3: 48).</summary>
    TruncatedTail = 0,

    /// <summary>Check 1 failed: the frame's offset field is not its real position in the file.</summary>
    OffsetMismatch = 1,

    /// <summary>Check 2 failed: the frame size is below the smallest header, or the frame does not fit in the rest of the file.</summary>
    InvalidFrameSize = 2,
}

/// <summary>Where and why the chain of frames broke before the end of the file (design section 6.3). Reading of that file stops there.</summary>
/// <param name="Position">The position in the file, in bytes, of the frame that could not be read.</param>
/// <param name="BytesLeft">The bytes from that position to the end of the file.</param>
/// <param name="Reason">Why the chain broke.</param>
public sealed record ChainBreak(long Position, long BytesLeft, ChainBreakReason Reason);
