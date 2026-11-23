using System.Buffers.Binary;

namespace Sl23.Reader;

/// <summary>The 8-byte header at the start of an SL2 or SL3 file (design section 6.2).</summary>
/// <param name="FormatCode">The format code: 1 = SLG, 2 = SL2, 3 = SL3. The frame layout comes from this field, not from the file extension.</param>
/// <param name="Word2">Header word at byte 2; its meaning is not established, reported raw.</param>
/// <param name="Word4">Header word at byte 4; not established, reported raw.</param>
/// <param name="Word6">Header word at byte 6; not established, reported raw.</param>
public readonly record struct FileHeader(ushort FormatCode, ushort Word2, ushort Word4, ushort Word6)
{
    /// <summary>The size of the file header in bytes; frames start at this file position.</summary>
    public const int Size = 8;

    /// <summary>Gets the format the code names; <see cref="FileFormat.Unknown"/> for any other code.</summary>
    public FileFormat Format => FormatCode switch
    {
        1 => FileFormat.Slg,
        2 => FileFormat.Sl2,
        3 => FileFormat.Sl3,
        _ => FileFormat.Unknown,
    };

    /// <summary>Gets a value indicating whether frames are read from this file: only SL2 and SL3 are.</summary>
    public bool HasReadableFrames => Format is FileFormat.Sl2 or FileFormat.Sl3;

    /// <summary>Reads the header from the first bytes of a file.</summary>
    /// <param name="bytes">The bytes at the start of the file; only the first 8 are read.</param>
    /// <param name="header">The header, when there are at least 8 bytes.</param>
    /// <returns><see langword="false"/> when fewer than 8 bytes are given: such a file holds no frames.</returns>
    public static bool TryParse(ReadOnlySpan<byte> bytes, out FileHeader header)
    {
        if (bytes.Length < Size)
        {
            header = default;
            return false;
        }

        header = new FileHeader(
            BinaryPrimitives.ReadUInt16LittleEndian(bytes),
            BinaryPrimitives.ReadUInt16LittleEndian(bytes[2..]),
            BinaryPrimitives.ReadUInt16LittleEndian(bytes[4..]),
            BinaryPrimitives.ReadUInt16LittleEndian(bytes[6..]));
        return true;
    }
}
