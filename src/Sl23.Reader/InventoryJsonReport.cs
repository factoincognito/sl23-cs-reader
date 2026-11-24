namespace Sl23.Reader;

/// <summary>Writes the inventory as JSON with the same content as the text report (design sections 4 and 7.2).</summary>
public static class InventoryJsonReport
{
    /// <summary>Writes the report.</summary>
    /// <param name="output">Where the report goes.</param>
    /// <param name="files">The files, in the order given.</param>
    /// <param name="options">What to show.</param>
    public static void Write(TextWriter output, IReadOnlyList<FileReport> files, ReportOptions options) => throw new NotImplementedException();
}
