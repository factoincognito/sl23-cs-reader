namespace Sl23.Reader;

/// <summary>Why a file has no inventory (design section 4: a file that cannot be read is reported with the reason).</summary>
public enum FileProblem
{
    /// <summary>The file was read.</summary>
    None = 0,

    /// <summary>The path does not name a file.</summary>
    NotFound = 1,

    /// <summary>The file may not be opened.</summary>
    AccessDenied = 2,

    /// <summary>The file could not be read to the end.</summary>
    ReadFailed = 3,

    /// <summary>The file is shorter than the 8-byte file header.</summary>
    TooShortForHeader = 4,

    /// <summary>The file header names SLG or an unknown format, which is not read further.</summary>
    FormatNotRead = 5,
}

/// <summary>One input file's result, for the report.</summary>
/// <param name="Path">The path as the user gave it; shown only with <see cref="ReportOptions.IncludeDetails"/>.</param>
/// <param name="Inventory">The inventory, when the file was read.</param>
/// <param name="Problem">Why there is no inventory, or <see cref="FileProblem.None"/>.</param>
/// <param name="Size">The file size in bytes, when known.</param>
/// <param name="Header">The file header, when the file has one.</param>
public sealed record FileReport(string Path, FileInventory? Inventory, FileProblem Problem = FileProblem.None, long? Size = null, FileHeader? Header = null);

/// <summary>What a report shows beyond the default.</summary>
/// <param name="IncludeDetails">Whether the report adds file names, depths and the list of range-limit pairs; without it the report holds structure and counts only (design section 4).</param>
public sealed record ReportOptions(bool IncludeDetails = false);

/// <summary>Writes the inventory as a readable text report (design section 7.2).</summary>
public static class InventoryTextReport
{
    /// <summary>Writes the report.</summary>
    /// <param name="output">Where the report goes.</param>
    /// <param name="files">The files, in the order given.</param>
    /// <param name="options">What to show.</param>
    public static void Write(TextWriter output, IReadOnlyList<FileReport> files, ReportOptions options) => throw new NotImplementedException();
}
