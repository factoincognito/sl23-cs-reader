using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Sl23.Reader;

/// <summary>
/// Writes the inventory as JSON with the same content as the text report (design sections 4 and 7.2).
/// The writer uses relaxed escaping, so the output is JSON for a file or a terminal and must not be embedded in HTML.
/// </summary>
public static class InventoryJsonReport
{
    private static readonly JsonWriterOptions WriterOptions = new()
    {
        Indented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>Writes the report: one JSON document, indented, in UTF-16 text; numbers as JSON numbers.</summary>
    /// <param name="output">Where the report goes.</param>
    /// <param name="files">The files, in the order given.</param>
    /// <param name="options">What to show; the default holds no file name, depth or pair list, as the text report.</param>
    public static void Write(TextWriter output, IReadOnlyList<FileReport> files, ReportOptions options)
    {
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(files);
        ArgumentNullException.ThrowIfNull(options);

        using var stream = new MemoryStream();
        using (var json = new Utf8JsonWriter(stream, WriterOptions))
        {
            json.WriteStartObject();
            json.WriteNumber("fileCount", files.Count);
            json.WriteBoolean("detailsIncluded", options.IncludeDetails);
            json.WriteStartArray("files");
            for (var i = 0; i < files.Count; i++)
            {
                WriteFile(json, files[i], i + 1, options);
            }

            json.WriteEndArray();
            json.WriteEndObject();
        }

        output.WriteLine(Encoding.UTF8.GetString(stream.ToArray()));
    }

    private static void WriteFile(Utf8JsonWriter json, FileReport file, int number, ReportOptions options)
    {
        json.WriteStartObject();
        json.WriteNumber("number", number);
        if (options.IncludeDetails)
        {
            json.WriteString("name", file.Path);
        }

        if (file.Inventory is not { } inventory)
        {
            WriteUnread(json, file);
            json.WriteEndObject();
            return;
        }

        json.WriteNumber("size", inventory.FileSize);
        json.WriteNull("problem");
        WriteHeader(json, inventory.Header);
        json.WriteNumber("frames", inventory.FrameCount);
        WriteCounts(json, "layouts", "headerSize", inventory.Layouts);
        json.WriteNumber("layoutsUntracked", inventory.FramesWithUntrackedLayouts);
        WriteChain(json, inventory.Break);
        WriteTime(json, inventory.Time);
        WriteChecks(json, inventory.Checks);
        json.WriteStartArray("channels");
        foreach (var channel in inventory.Channels)
        {
            WriteChannel(json, channel, options);
        }

        json.WriteEndArray();
        json.WriteEndObject();
    }

    private static void WriteUnread(Utf8JsonWriter json, FileReport file)
    {
        if (file.Size is { } size)
        {
            json.WriteNumber("size", size);
        }
        else
        {
            json.WriteNull("size");
        }

        json.WriteString("problem", file.Problem switch
        {
            FileProblem.NotFound => "notFound",
            FileProblem.AccessDenied => "accessDenied",
            FileProblem.TooShortForHeader => "tooShortForHeader",
            FileProblem.FormatNotRead => "formatNotRead",
            _ => "readFailed",
        });
        if (file.Header is { } header)
        {
            WriteHeader(json, header);
        }
        else
        {
            json.WriteNull("header");
        }
    }

    private static void WriteHeader(Utf8JsonWriter json, FileHeader header)
    {
        json.WriteStartObject("header");
        json.WriteNumber("formatCode", header.FormatCode);
        json.WriteString("format", header.Format switch
        {
            FileFormat.Sl2 => "SL2",
            FileFormat.Sl3 => "SL3",
            FileFormat.Slg => "SLG",
            _ => "unknown",
        });
        json.WriteNumber("word2", header.Word2);
        json.WriteNumber("word4", header.Word4);
        json.WriteNumber("word6", header.Word6);
        json.WriteEndObject();
    }

    private static void WriteCounts<T>(Utf8JsonWriter json, string name, string keyName, IReadOnlyDictionary<T, long> counts)
        where T : notnull
    {
        json.WriteStartArray(name);
        foreach (var (key, count) in counts)
        {
            json.WriteStartObject();
            json.WriteNumber(keyName, Convert.ToInt64(key, System.Globalization.CultureInfo.InvariantCulture));
            json.WriteNumber("count", count);
            json.WriteEndObject();
        }

        json.WriteEndArray();
    }

    private static void WriteChain(Utf8JsonWriter json, ChainBreak? chainBreak)
    {
        json.WriteStartObject("chain");
        json.WriteBoolean("endsAtEndOfFile", chainBreak is null);
        if (chainBreak is null)
        {
            json.WriteNull("break");
        }
        else
        {
            json.WriteStartObject("break");
            json.WriteNumber("position", chainBreak.Position);
            json.WriteNumber("bytesLeft", chainBreak.BytesLeft);
            json.WriteString("reason", chainBreak.Reason switch
            {
                ChainBreakReason.TruncatedTail => "truncatedTail",
                ChainBreakReason.OffsetMismatch => "offsetMismatch",
                _ => "invalidFrameSize",
            });
            json.WriteEndObject();
        }

        json.WriteEndObject();
    }

    private static void WriteTime(Utf8JsonWriter json, TimeSummary? time)
    {
        if (time is null)
        {
            json.WriteNull("time");
            return;
        }

        json.WriteStartObject("time");
        json.WriteNumber("firstRaw", time.FirstRaw);
        json.WriteNumber("firstSigned", time.FirstSigned);
        json.WriteNumber("lastRaw", time.LastRaw);
        json.WriteNumber("lastSigned", time.LastSigned);
        json.WriteNumber("framesBelowZeroAsSigned", time.NegativeAsSigned);
        json.WriteEndObject();
    }

    private static void WriteChecks(Utf8JsonWriter json, IReadOnlyDictionary<int, CheckResult> checks)
    {
        json.WriteStartArray("checks");
        foreach (var (number, check) in checks.OrderBy(c => c.Key))
        {
            json.WriteStartObject();
            json.WriteNumber("number", number);
            json.WriteString("name", InventoryTextReport.CheckName(number));
            json.WriteNumber("passed", check.Passed);
            json.WriteNumber("failed", check.Failed);
            json.WriteNumber("countedApart", check.CountedApart);
            json.WriteStartArray("firstFailingPositions");
            foreach (var position in check.FirstFailingPositions)
            {
                json.WriteNumberValue(position);
            }

            json.WriteEndArray();
            json.WriteNumber("failuresLeftOut", check.FailuresLeftOut);
            json.WriteEndObject();
        }

        json.WriteEndArray();
    }

    private static void WriteChannel(Utf8JsonWriter json, ChannelInventory channel, ReportOptions options)
    {
        json.WriteStartObject();
        json.WriteNumber("id", channel.ChannelId);
        json.WriteString("label", channel.Label);
        json.WriteNumber("frames", channel.FrameCount);
        WriteNullableNumber(json, "firstFrameIndex", channel.FirstFrameIndex);
        WriteNullableNumber(json, "lastFrameIndex", channel.LastFrameIndex);
        json.WriteNumber("indexGaps", channel.Checks[6].Failed);
        WriteCounts(json, "headerSizes", "headerSize", channel.HeaderSizes);
        json.WriteNumber("headerSizesUntracked", channel.FramesWithUntrackedHeaderSizes);
        WriteCounts(json, "frameVersions", "version", channel.FrameVersions);
        json.WriteNumber("frameVersionsUntracked", channel.FramesWithUntrackedFrameVersions);
        json.WriteNumber("distinctRangeLimitPairs", channel.DistinctRangeLimitPairs);
        json.WriteNumber("framesWithoutValidRangeLimits", channel.FramesWithoutValidRangeLimits);
        json.WriteNumber("framesWithUntrackedRangeLimits", channel.FramesWithUntrackedRangeLimits);
        json.WriteNumber("framesWithValidPosition", channel.FramesWithValidPosition);
        WriteChecks(json, channel.Checks);
        if (options.IncludeDetails)
        {
            WriteDetails(json, channel);
        }

        json.WriteEndObject();
    }

    private static void WriteDetails(Utf8JsonWriter json, ChannelInventory channel)
    {
        WriteNullableNumber(json, "depthMinMetres", channel.DepthMinMetres);
        WriteNullableNumber(json, "depthMaxMetres", channel.DepthMaxMetres);
        json.WriteStartArray("rangeLimitPairs");
        foreach (var (pair, count) in channel.RangeLimitPairs.OrderBy(p => p.Key.Upper).ThenBy(p => p.Key.Lower))
        {
            json.WriteStartObject();
            json.WriteNumber("upperMetres", pair.Upper);
            json.WriteNumber("lowerMetres", pair.Lower);
            json.WriteNumber("count", count);
            json.WriteEndObject();
        }

        json.WriteEndArray();
    }

    private static void WriteNullableNumber(Utf8JsonWriter json, string name, uint? value)
    {
        if (value is { } number)
        {
            json.WriteNumber(name, number);
        }
        else
        {
            json.WriteNull(name);
        }
    }

    private static void WriteNullableNumber(Utf8JsonWriter json, string name, double? value)
    {
        if (value is { } number)
        {
            json.WriteNumber(name, number);
        }
        else
        {
            json.WriteNull(name);
        }
    }
}
