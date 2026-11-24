using System.Globalization;
using System.Text.Json;

namespace Sl23.Reader.Tests;

public class InventoryJsonReportTests
{
    private const string SecretPath = "/home/adam/secret_2025-06-12_08.15.sl3";

    private static readonly FileHeader Sl3Header = new(3, 0, 0, 0);

    private static FrameRecord Frame(long position, ushort channel = 0, long headerSize = 168, int frameSize = 200) => new()
    {
        Format = FileFormat.Sl3,
        Position = position,
        OffsetField = unchecked((uint)position),
        HeaderSize = headerSize,
        FrameSize = frameSize,
        EchoSize = frameSize - headerSize,
        ChannelId = channel,
    };

    private static FileInventory Inventory(ChainBreak? chainBreak, long size, params FrameRecord[] frames)
    {
        var builder = new InventoryBuilder(Sl3Header, size);
        foreach (var frame in frames)
        {
            builder.Add(frame);
        }

        return builder.Complete(chainBreak);
    }

    private static FrameRecord Good(long position, uint index, uint time, ushort? previousSize) => Frame(position) with
    {
        FrameIndex = index,
        PreviousFrameSize = previousSize,
        TimeRaw = time,
        FrameVersion = 10,
        UpperLimitFeet = 0f,
        LowerLimitFeet = 100f,
        DepthFeet = 20f,
        Flags = Units.PositionValidFlag,
        X = 1000,
        Y = 2000,
    };

    private static FileInventory Sample() => Inventory(
        null,
        546,
        Good(8, 0, 100, null),
        Good(208, 1, 101, 200),
        Frame(408, 7, 128, 138));

    private static string Text(ReportOptions options, params FileReport[] files)
    {
        using var writer = new StringWriter();
        InventoryJsonReport.Write(writer, files, options);
        return writer.ToString();
    }

    private static JsonElement Root(ReportOptions options, params FileReport[] files)
    {
        using var document = JsonDocument.Parse(Text(options, files));
        return document.RootElement.Clone();
    }

    private static JsonElement First(params FileReport[] files) => Root(new ReportOptions(), files).GetProperty("files")[0];

    [Fact]
    public void NoFilesGivesAnEmptyList()
    {
        var root = Root(new ReportOptions());

        Assert.Equal(0, root.GetProperty("fileCount").GetInt32());
        Assert.Equal(0, root.GetProperty("files").GetArrayLength());
        Assert.False(root.GetProperty("detailsIncluded").GetBoolean());
    }

    [Fact]
    public void WritesTheFileLevelFacts()
    {
        var file = First(new FileReport(SecretPath, Sample()));

        Assert.Equal(1, file.GetProperty("number").GetInt32());
        Assert.Equal(546, file.GetProperty("size").GetInt64());
        Assert.Equal(JsonValueKind.Null, file.GetProperty("problem").ValueKind);
        var header = file.GetProperty("header");
        Assert.Equal(3, header.GetProperty("formatCode").GetInt32());
        Assert.Equal("SL3", header.GetProperty("format").GetString());
        Assert.Equal([0, 0, 0], new[] { header.GetProperty("word2").GetInt32(), header.GetProperty("word4").GetInt32(), header.GetProperty("word6").GetInt32() });
        Assert.Equal(3, file.GetProperty("frames").GetInt64());
        var layouts = file.GetProperty("layouts");
        Assert.Equal(2, layouts.GetArrayLength());
        Assert.Equal(128, layouts[0].GetProperty("headerSize").GetInt64());
        Assert.Equal(1, layouts[0].GetProperty("count").GetInt64());
        Assert.Equal(168, layouts[1].GetProperty("headerSize").GetInt64());
        Assert.Equal(2, layouts[1].GetProperty("count").GetInt64());
        Assert.Equal(0, file.GetProperty("layoutsUntracked").GetInt64());
        Assert.True(file.GetProperty("chain").GetProperty("endsAtEndOfFile").GetBoolean());
        Assert.Equal(JsonValueKind.Null, file.GetProperty("chain").GetProperty("break").ValueKind);
    }

    [Fact]
    public void WritesTheTimeCounterInBothReadings()
    {
        var time = First(new FileReport("f", Sample())).GetProperty("time");

        Assert.Equal(100u, time.GetProperty("firstRaw").GetUInt32());
        Assert.Equal(100, time.GetProperty("firstSigned").GetInt32());
        Assert.Equal(101u, time.GetProperty("lastRaw").GetUInt32());
        Assert.Equal(101, time.GetProperty("lastSigned").GetInt32());
        Assert.Equal(0, time.GetProperty("framesBelowZeroAsSigned").GetInt64());
    }

    [Fact]
    public void TheTimeCounterIsNullWhenNoFrameHasOneAndNegativeValuesAreCounted()
    {
        var none = First(new FileReport("f", Inventory(null, 208, Frame(8))));
        var negative = First(new FileReport("f", Inventory(null, 408, Frame(8) with { TimeRaw = 0xFFFFFFF0 }, Frame(208) with { TimeRaw = 5 })));

        Assert.Equal(JsonValueKind.Null, none.GetProperty("time").ValueKind);
        Assert.Equal(4294967280u, negative.GetProperty("time").GetProperty("firstRaw").GetUInt32());
        Assert.Equal(-16, negative.GetProperty("time").GetProperty("firstSigned").GetInt32());
        Assert.Equal(1, negative.GetProperty("time").GetProperty("framesBelowZeroAsSigned").GetInt64());
    }

    [Fact]
    public void WritesAllNineChecksWithTheirCounts()
    {
        var checks = First(new FileReport("f", Sample())).GetProperty("checks");

        Assert.Equal(9, checks.GetArrayLength());
        Assert.Equal([1, 2, 3, 4, 5, 6, 7, 8, 9], checks.EnumerateArray().Select(c => c.GetProperty("number").GetInt32()));
        Assert.Equal("offset field is the frame's position", checks[0].GetProperty("name").GetString());
        Assert.Equal(3, checks[0].GetProperty("passed").GetInt64());
        Assert.Equal(0, checks[0].GetProperty("failed").GetInt64());
        Assert.Equal(0, checks[0].GetProperty("countedApart").GetInt64());
        Assert.Equal(0, checks[0].GetProperty("firstFailingPositions").GetArrayLength());
        Assert.Equal(0, checks[0].GetProperty("failuresLeftOut").GetInt64());
        Assert.Equal(2, checks[8].GetProperty("passed").GetInt64());
    }

    [Fact]
    public void FailingPositionsApartCountsAndLeftOutCountsAreWritten()
    {
        var frames = Enumerable.Range(0, 13).Select(i => Frame(8 + (i * 200L), 0, 150) with { PreviousFrameOffsets = [0, 0, 0, 0, 0, 0] }).ToArray();

        var checks = First(new FileReport("f", Inventory(null, 8 + (13 * 200), frames))).GetProperty("checks");

        Assert.Equal(13, checks[2].GetProperty("failed").GetInt64());
        Assert.Equal(10, checks[2].GetProperty("firstFailingPositions").GetArrayLength());
        Assert.Equal(8, checks[2].GetProperty("firstFailingPositions")[0].GetInt64());
        Assert.Equal(3, checks[2].GetProperty("failuresLeftOut").GetInt64());
        Assert.Equal(13 * 4, checks[6].GetProperty("countedApart").GetInt64());
    }

    [Fact]
    public void AGapInTheFrameIndexIsCountedAsAnIndexGap()
    {
        var inventory = Inventory(
            null,
            608,
            Frame(8) with { FrameIndex = 1 },
            Frame(208) with { FrameIndex = 2 },
            Frame(408) with { FrameIndex = 9 });

        var channel = First(new FileReport("f", inventory)).GetProperty("channels")[0];

        Assert.Equal(1, channel.GetProperty("indexGaps").GetInt64());
        Assert.Equal(9u, channel.GetProperty("lastFrameIndex").GetUInt32());
    }

    [Fact]
    public void TheTextIsNotEscapedMoreThanJsonNeeds()
    {
        var text = Text(new ReportOptions(true), new FileReport("\u00e5\u00e4\u00f6.sl2", Sample()));

        Assert.Contains("\"offset field is the frame's position\"", text, StringComparison.Ordinal);
        Assert.Contains("\"name\": \"\u00e5\u00e4\u00f6.sl2\"", text, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(ChainBreakReason.TruncatedTail, "truncatedTail")]
    [InlineData(ChainBreakReason.OffsetMismatch, "offsetMismatch")]
    [InlineData(ChainBreakReason.InvalidFrameSize, "invalidFrameSize")]
    public void ABrokenChainIsWrittenWithPositionBytesLeftAndReason(ChainBreakReason reason, string text)
    {
        var inventory = Inventory(new ChainBreak(408, 30, reason), 438, Frame(8), Frame(208));

        var chain = First(new FileReport("f", inventory)).GetProperty("chain");

        Assert.False(chain.GetProperty("endsAtEndOfFile").GetBoolean());
        Assert.Equal(408, chain.GetProperty("break").GetProperty("position").GetInt64());
        Assert.Equal(30, chain.GetProperty("break").GetProperty("bytesLeft").GetInt64());
        Assert.Equal(text, chain.GetProperty("break").GetProperty("reason").GetString());
    }

    [Fact]
    public void WritesEachChannel()
    {
        var channels = First(new FileReport("f", Sample())).GetProperty("channels");

        Assert.Equal(2, channels.GetArrayLength());
        var channel = channels[0];
        Assert.Equal(0, channel.GetProperty("id").GetInt32());
        Assert.Equal("2D", channel.GetProperty("label").GetString());
        Assert.Equal(2, channel.GetProperty("frames").GetInt64());
        Assert.Equal(0u, channel.GetProperty("firstFrameIndex").GetUInt32());
        Assert.Equal(1u, channel.GetProperty("lastFrameIndex").GetUInt32());
        Assert.Equal(0, channel.GetProperty("indexGaps").GetInt64());
        Assert.Equal(168, channel.GetProperty("headerSizes")[0].GetProperty("headerSize").GetInt64());
        Assert.Equal(2, channel.GetProperty("headerSizes")[0].GetProperty("count").GetInt64());
        Assert.Equal(10, channel.GetProperty("frameVersions")[0].GetProperty("version").GetInt64());
        Assert.Equal(2, channel.GetProperty("frameVersions")[0].GetProperty("count").GetInt64());
        Assert.Equal(1, channel.GetProperty("distinctRangeLimitPairs").GetInt32());
        Assert.Equal(0, channel.GetProperty("framesWithoutValidRangeLimits").GetInt64());
        Assert.Equal(2, channel.GetProperty("framesWithValidPosition").GetInt64());
        Assert.Equal([1, 2, 3, 5, 6, 7, 8, 9], channel.GetProperty("checks").EnumerateArray().Select(c => c.GetProperty("number").GetInt32()));
        var reduced = channels[1];
        Assert.Equal("Unknown(7)", reduced.GetProperty("label").GetString());
        Assert.Equal(JsonValueKind.Null, reduced.GetProperty("firstFrameIndex").ValueKind);
        Assert.Equal(JsonValueKind.Null, reduced.GetProperty("lastFrameIndex").ValueKind);
        Assert.Equal(0, reduced.GetProperty("frameVersions").GetArrayLength());
        Assert.Equal(1, reduced.GetProperty("framesWithoutValidRangeLimits").GetInt64());
    }

    [Fact]
    public void FramesWithoutAValidPositionAreNotCountedAsHavingOne()
    {
        var frames = new[]
        {
            Frame(8) with { Flags = Units.PositionValidFlag, X = 1000, Y = 2000 },
            Frame(208) with { Flags = 0, X = 1000, Y = 2000 },
            Frame(408) with { Flags = Units.PositionValidFlag, X = 1000 },
        };

        var channel = First(new FileReport("f", Inventory(null, 608, frames))).GetProperty("channels")[0];

        Assert.Equal(3, channel.GetProperty("frames").GetInt64());
        Assert.Equal(1, channel.GetProperty("framesWithValidPosition").GetInt64());
    }

    [Fact]
    public void CapsAreReportedAsUntrackedCounts()
    {
        var total = InventoryBuilder.MaxRangeLimitPairs + 3;
        var frames = Enumerable.Range(0, total)
            .Select(i => Frame(8 + (i * 200L)) with { UpperLimitFeet = 0f, LowerLimitFeet = 100f + i })
            .ToArray();

        var channel = First(new FileReport("f", Inventory(null, 8 + (total * 200L), frames))).GetProperty("channels")[0];

        Assert.Equal(4096, channel.GetProperty("distinctRangeLimitPairs").GetInt32());
        Assert.Equal(3, channel.GetProperty("framesWithUntrackedRangeLimits").GetInt64());
    }

    [Fact]
    public void HeaderSizesAndVersionsBeyondTheCapAreReportedAsUntrackedCounts()
    {
        var total = InventoryBuilder.MaxDistinctValues + 6;
        var frames = Enumerable.Range(0, total)
            .Select(i => Frame(8 + (i * 400L), 0, 100 + i, 400) with { FrameVersion = (uint)i })
            .ToArray();

        var file = First(new FileReport("f", Inventory(null, 8 + (total * 400L), frames)));
        var channel = file.GetProperty("channels")[0];

        Assert.Equal(6, file.GetProperty("layoutsUntracked").GetInt64());
        Assert.Equal(6, channel.GetProperty("headerSizesUntracked").GetInt64());
        Assert.Equal(6, channel.GetProperty("frameVersionsUntracked").GetInt64());
    }

    [Fact]
    public void TheDefaultReportHoldsNoFileNameNoDepthAndNoRangeLimitPair()
    {
        var text = Text(new ReportOptions(), new FileReport(SecretPath, Sample()));

        Assert.DoesNotContain("secret", text);
        Assert.DoesNotContain("adam", text);
        Assert.DoesNotContain("2025", text);
        Assert.DoesNotContain("6.09", text);
        Assert.DoesNotContain("30.48", text);
        Assert.DoesNotContain("\"name\": \"/", text);
        Assert.DoesNotContain("depthMinMetres", text);
        Assert.DoesNotContain("depthMaxMetres", text);
        Assert.DoesNotContain("rangeLimitPairs", text);
        Assert.False(Root(new ReportOptions(), new FileReport(SecretPath, Sample())).GetProperty("detailsIncluded").GetBoolean());
    }

    [Fact]
    public void WithDetailsTheReportAddsTheFileNameTheDepthRangeAndThePairs()
    {
        var root = Root(new ReportOptions(true), new FileReport(SecretPath, Sample()));
        var file = root.GetProperty("files")[0];
        var channel = file.GetProperty("channels")[0];

        Assert.True(root.GetProperty("detailsIncluded").GetBoolean());
        Assert.Equal(SecretPath, file.GetProperty("name").GetString());
        Assert.Equal(Units.FeetToMetres(20f), channel.GetProperty("depthMinMetres").GetDouble(), 9);
        Assert.Equal(Units.FeetToMetres(20f), channel.GetProperty("depthMaxMetres").GetDouble(), 9);
        var pair = Assert.Single(channel.GetProperty("rangeLimitPairs").EnumerateArray());
        Assert.Equal(0, pair.GetProperty("upperMetres").GetDouble());
        Assert.Equal(Units.FeetToMetres(100f), pair.GetProperty("lowerMetres").GetDouble(), 9);
        Assert.Equal(2, pair.GetProperty("count").GetInt64());
        var reduced = file.GetProperty("channels")[1];
        Assert.Equal(JsonValueKind.Null, reduced.GetProperty("depthMinMetres").ValueKind);
        Assert.Equal(0, reduced.GetProperty("rangeLimitPairs").GetArrayLength());
    }

    [Fact]
    public void WithDetailsThePairsAreListedByUpperThenLowerLimit()
    {
        var inventory = Inventory(
            null,
            1000,
            Frame(8) with { UpperLimitFeet = 10f, LowerLimitFeet = 100f },
            Frame(208) with { UpperLimitFeet = 0f, LowerLimitFeet = 120f },
            Frame(408) with { UpperLimitFeet = 0f, LowerLimitFeet = 100f });

        var pairs = Root(new ReportOptions(true), new FileReport("f", inventory)).GetProperty("files")[0].GetProperty("channels")[0].GetProperty("rangeLimitPairs");

        Assert.Equal(
            [(0.0, 100.0), (0.0, 120.0), (10.0, 100.0)],
            pairs.EnumerateArray().Select(p => (Math.Round(p.GetProperty("upperMetres").GetDouble() / 0.3048, 3), Math.Round(p.GetProperty("lowerMetres").GetDouble() / 0.3048, 3))));
    }

    [Theory]
    [InlineData(FileProblem.NotFound, "notFound")]
    [InlineData(FileProblem.AccessDenied, "accessDenied")]
    [InlineData(FileProblem.ReadFailed, "readFailed")]
    public void AFileThatCannotBeReadIsWrittenWithTheReasonAndWithoutItsPath(FileProblem problem, string text)
    {
        var report = Text(new ReportOptions(), new FileReport(SecretPath, null, problem));
        var file = First(new FileReport(SecretPath, null, problem));

        Assert.Equal(text, file.GetProperty("problem").GetString());
        Assert.DoesNotContain("secret", report);
        Assert.Equal(JsonValueKind.Null, file.GetProperty("size").ValueKind);
        Assert.False(file.TryGetProperty("frames", out _));
    }

    [Fact]
    public void AFileThatCannotBeReadShowsItsNameWithDetails()
    {
        var file = Root(new ReportOptions(true), new FileReport(SecretPath, null, FileProblem.NotFound)).GetProperty("files")[0];

        Assert.Equal(SecretPath, file.GetProperty("name").GetString());
    }

    [Fact]
    public void AFileShorterThanTheHeaderAndAFileOfAnotherFormatAreWrittenAsNotRead()
    {
        var root = Root(
            new ReportOptions(),
            new FileReport("a", null, FileProblem.TooShortForHeader, Size: 5),
            new FileReport("b", null, FileProblem.FormatNotRead, Size: 1500, Header: new FileHeader(1, 7, 8, 9)),
            new FileReport("c", null, FileProblem.FormatNotRead, Size: 20, Header: new FileHeader(9, 0, 0, 0)));
        var files = root.GetProperty("files");

        Assert.Equal("tooShortForHeader", files[0].GetProperty("problem").GetString());
        Assert.Equal(5, files[0].GetProperty("size").GetInt64());
        Assert.Equal(JsonValueKind.Null, files[0].GetProperty("header").ValueKind);
        Assert.Equal("formatNotRead", files[1].GetProperty("problem").GetString());
        Assert.Equal("SLG", files[1].GetProperty("header").GetProperty("format").GetString());
        Assert.Equal(7, files[1].GetProperty("header").GetProperty("word2").GetInt32());
        Assert.Equal("unknown", files[2].GetProperty("header").GetProperty("format").GetString());
        Assert.Equal(3, root.GetProperty("fileCount").GetInt32());
        Assert.Equal([1, 2, 3], files.EnumerateArray().Select(f => f.GetProperty("number").GetInt32()));
    }

    [Fact]
    public void AFileWithNeitherAnInventoryNorAReasonIsWrittenAsReadFailed()
    {
        var file = First(new FileReport("a", null));

        Assert.Equal("readFailed", file.GetProperty("problem").GetString());
    }

    [Fact]
    public void ANameWithSpecialCharactersRoundTrips()
    {
        const string Name = "C:\\data\\\"quoted\"\nline\t\u00e5\u00e4\u00f6.sl2";

        var file = Root(new ReportOptions(true), new FileReport(Name, null, FileProblem.NotFound)).GetProperty("files")[0];

        Assert.Equal(Name, file.GetProperty("name").GetString());
    }

    [Fact]
    public void ANameWithAnUnpairedSurrogateIsWrittenWithTheReplacementCharacter()
    {
        // Built here, not in InlineData: the test framework does not carry an unpaired surrogate through its own serialisation.
        (string Name, string Written)[] cases =
        [
            ("a\uD800b", "a\uFFFDb"),
            ("a\uDC00b", "a\uFFFDb"),
            ("a\uD800", "a\uFFFD"),
            ("\uDC00\uD800", "\uFFFD\uFFFD"),
            ("a\uD83D\uDE00b", "a\uD83D\uDE00b"),
        ];

        foreach (var (name, written) in cases)
        {
            var file = Root(new ReportOptions(true), new FileReport(name, null, FileProblem.NotFound)).GetProperty("files")[0];

            Assert.Equal(written, file.GetProperty("name").GetString());
        }
    }

    [Fact]
    public void NumbersAreWrittenTheSameInEveryCulture()
    {
        var before = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("sv-SE");
            var channel = Root(new ReportOptions(true), new FileReport("f", Sample())).GetProperty("files")[0].GetProperty("channels")[0];

            Assert.Equal(Units.FeetToMetres(20f), channel.GetProperty("depthMinMetres").GetDouble(), 9);
        }
        finally
        {
            CultureInfo.CurrentCulture = before;
        }
    }

    [Fact]
    public void RefusesMissingArguments()
    {
        Assert.Throws<ArgumentNullException>(() => InventoryJsonReport.Write(null!, [], new ReportOptions()));
        Assert.Throws<ArgumentNullException>(() => InventoryJsonReport.Write(new StringWriter(), null!, new ReportOptions()));
        Assert.Throws<ArgumentNullException>(() => InventoryJsonReport.Write(new StringWriter(), [], null!));
    }
}
