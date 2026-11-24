using System.Globalization;

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
    private static readonly string[] CheckNames =
    [
        string.Empty,
        "offset field is the frame's position",
        "frame size fits the file and is at least the smallest header",
        "header size is as expected",
        "chain ends exactly at the end of the file",
        "previous frame size is the size of the frame before",
        "frame index follows the previous one of the channel",
        "previous frame offsets point to the latest frame of their channel",
        "time does not decrease within the channel",
        "depth lies between the range limits",
    ];

    /// <summary>Writes the report: numbers in the invariant culture, one line per fact.</summary>
    /// <param name="output">Where the report goes.</param>
    /// <param name="files">The files, in the order given.</param>
    /// <param name="options">What to show.</param>
    public static void Write(TextWriter output, IReadOnlyList<FileReport> files, ReportOptions options)
    {
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(files);
        ArgumentNullException.ThrowIfNull(options);

        output.WriteLine(Inv($"Inventory of {Count(files.Count, "file", "files")}"));
        for (var i = 0; i < files.Count; i++)
        {
            output.WriteLine();
            WriteFile(output, files[i], i + 1, files.Count, options);
        }

        if (!options.IncludeDetails)
        {
            output.WriteLine();
            output.WriteLine("Not shown unless asked for: file names, depths, the list of range-limit pairs.");
        }
    }

    private static string Inv(FormattableString text) => text.ToString(CultureInfo.InvariantCulture);

    private static string Count(long n, string one, string many) => Inv($"{n} {(n == 1 ? one : many)}");

    private static string Name(FileReport file, ReportOptions options) => options.IncludeDetails ? $", name: {file.Path}" : string.Empty;

    private static void WriteFile(TextWriter output, FileReport file, int number, int total, ReportOptions options)
    {
        var title = Inv($"File {number} of {total}:");
        if (file.Inventory is not { } inventory)
        {
            output.WriteLine(title + " " + Problem(file) + Name(file, options));
            return;
        }

        output.WriteLine(title + Inv($" {inventory.FileSize} bytes") + Name(file, options));
        var header = inventory.Header;
        output.WriteLine(Inv($"  File header: format {FormatName(header.Format)} (code {header.FormatCode}), words {header.Word2} {header.Word4} {header.Word6}"));
        output.WriteLine(Inv($"  Frames: {inventory.FrameCount}"));
        output.WriteLine("  Layouts: " + Join(inventory.Layouts, size => Inv($"{size} bytes")));
        output.WriteLine("  Chain: " + Chain(inventory.Break));
        output.WriteLine("  Time counter: " + Time(inventory.Time));
        output.WriteLine("  Health checks (passed / failed):");
        foreach (var check in inventory.Checks.OrderBy(c => c.Key))
        {
            output.WriteLine("    " + CheckLine(check.Key, check.Value));
        }

        foreach (var channel in inventory.Channels)
        {
            WriteChannel(output, channel, options);
        }
    }

    private static string Problem(FileReport file) => file.Problem switch
    {
        FileProblem.NotFound => "could not be read: file not found",
        FileProblem.AccessDenied => "could not be read: access denied",
        FileProblem.ReadFailed => "could not be read: read error",
        FileProblem.TooShortForHeader => Inv($"{file.Size} bytes, not read: shorter than the 8-byte file header"),
        FileProblem.FormatNotRead when file.Header is { } header =>
            Inv($"{file.Size} bytes, not read: file header format code {header.FormatCode} ({(header.Format == FileFormat.Slg ? "SLG" : "unknown")}), not read further"),
        _ => "could not be read",
    };

    private static string FormatName(FileFormat format) => format switch
    {
        FileFormat.Sl2 => "SL2",
        FileFormat.Sl3 => "SL3",
        FileFormat.Slg => "SLG",
        _ => "unknown",
    };

    private static string Join<T>(IReadOnlyDictionary<T, long> counts, Func<T, string> label) where T : notnull =>
        string.Join(", ", counts.Select(c => Inv($"{label(c.Key)} x {c.Value}")));

    private static string Chain(ChainBreak? chainBreak)
    {
        if (chainBreak is null)
        {
            return "ends at the end of the file";
        }

        var reason = chainBreak.Reason switch
        {
            ChainBreakReason.TruncatedTail => "fewer bytes left than a frame header needs",
            ChainBreakReason.OffsetMismatch => "offset field is not the position",
            _ => "frame size is below the smallest header or past the end of the file",
        };
        return Inv($"breaks at byte {chainBreak.Position}, {Count(chainBreak.BytesLeft, "byte", "bytes")} left ({reason})");
    }

    private static string Time(TimeSummary? time) => time is null
        ? "no frame has one"
        : Inv($"first {time.FirstRaw} (signed {time.FirstSigned}), last {time.LastRaw} (signed {time.LastSigned}), {Count(time.NegativeAsSigned, "frame", "frames")} below zero as signed");

    private static string CheckLine(int number, CheckResult check)
    {
        var line = Inv($"{number} {CheckNames[number]}: {check.Passed} / {check.Failed}");
        if (check.CountedApart > 0)
        {
            line += Inv($", {check.CountedApart} counted apart");
        }

        if (check.Failed > 0)
        {
            line += "; first failing at bytes " + string.Join(", ", check.FirstFailingPositions.Select(p => p.ToString(CultureInfo.InvariantCulture)));
            if (check.FailuresLeftOut > 0)
            {
                line += Inv($" ({check.FailuresLeftOut} more not listed)");
            }
        }

        return line;
    }

    private static void WriteChannel(TextWriter output, ChannelInventory channel, ReportOptions options)
    {
        output.WriteLine(Inv($"  Channel {channel.ChannelId} ({channel.Label}): {Count(channel.FrameCount, "frame", "frames")}"));
        output.WriteLine("    Frame index: " + (channel.FirstFrameIndex is { } first && channel.LastFrameIndex is { } last
            ? Inv($"first {first}, last {last}, {Count(channel.Checks[6].Failed, "gap", "gaps")}")
            : "none"));
        output.WriteLine("    Header sizes: " + Join(channel.HeaderSizes, size => Inv($"{size} bytes")));
        if (channel.FrameVersions.Count > 0)
        {
            output.WriteLine("    Frame versions: " + Join(channel.FrameVersions, version => Inv($"{version}")));
        }

        var ranges = Inv($"    Range limits: {Count(channel.DistinctRangeLimitPairs, "distinct pair", "distinct pairs")}, {Count(channel.FramesWithoutValidRangeLimits, "frame", "frames")} without a valid pair");
        if (channel.FramesWithUntrackedRangeLimits > 0)
        {
            ranges += Inv($", {Count(channel.FramesWithUntrackedRangeLimits, "frame", "frames")} beyond the cap not tracked");
        }

        output.WriteLine(ranges);
        if (options.IncludeDetails)
        {
            WriteDetails(output, channel);
        }

        var share = 100.0 * channel.FramesWithValidPosition / channel.FrameCount;
        output.WriteLine(Inv($"    Frames with a valid position: {channel.FramesWithValidPosition} of {channel.FrameCount} ({share:0.0}%)"));
        output.WriteLine("    Checks (passed / failed): " + string.Join(", ", channel.Checks.OrderBy(c => c.Key).Select(c => Inv($"{c.Key} {c.Value.Passed}/{c.Value.Failed}"))));
    }

    private static void WriteDetails(TextWriter output, ChannelInventory channel)
    {
        var pairs = channel.RangeLimitPairs.OrderBy(p => p.Key.Upper).ThenBy(p => p.Key.Lower)
            .Select(p => Inv($"{p.Key.Upper:0.00} to {p.Key.Lower:0.00} x {p.Value}"));
        output.WriteLine("    Range limit pairs (m, upper to lower): " + (channel.RangeLimitPairs.Count == 0 ? "none" : string.Join(", ", pairs)));
        output.WriteLine("    Depth (m): " + (channel.DepthMinMetres is { } min && channel.DepthMaxMetres is { } max
            ? Inv($"min {min:0.00}, max {max:0.00}")
            : "none"));
    }
}
