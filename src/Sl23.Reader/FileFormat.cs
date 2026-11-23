namespace Sl23.Reader;

/// <summary>The format named by the file header's format code (design section 6.2).</summary>
public enum FileFormat
{
    /// <summary>Any code other than 1, 2 or 3; reported as such and not read further.</summary>
    Unknown = 0,

    /// <summary>Code 1, SLG; reported as such and not read further.</summary>
    Slg = 1,

    /// <summary>Code 2, SL2 (144-byte frame header).</summary>
    Sl2 = 2,

    /// <summary>Code 3, SL3 (frame header of 168, 184 or 128 bytes).</summary>
    Sl3 = 3,
}
