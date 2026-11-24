using System.Globalization;

namespace Sl23.Reader.Tests;

public class InventoryTextReportTests
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

    private static string Report(ReportOptions options, params FileReport[] files)
    {
        using var writer = new StringWriter();
        InventoryTextReport.Write(writer, files, options);
        return writer.ToString().ReplaceLineEndings("\n");
    }

    private static string Default(params FileReport[] files) => Report(new ReportOptions(), files);

    [Fact]
    public void NoFilesGivesAHeadingOnly()
    {
        Assert.StartsWith("Inventory of 0 files\n", Default());
    }

    [Fact]
    public void WritesTheWholeReportOfASmallFile()
    {
        var text = Default(new FileReport(SecretPath, Sample()));

        var expected = """
            Inventory of 1 file

            File 1 of 1: 546 bytes
              File header: format SL3 (code 3), words 0 0 0
              Frames: 3
              Layouts: 128 bytes x 1, 168 bytes x 2
              Chain: ends at the end of the file
              Time counter: first 100 (signed 100), last 101 (signed 101), 0 frames below zero as signed
              Health checks (passed / failed):
                1 offset field is the frame's position: 3 / 0
                2 frame size fits the file and is at least the smallest header: 3 / 0
                3 header size is as expected: 3 / 0
                4 chain ends exactly at the end of the file: 1 / 0
                5 previous frame size is the size of the frame before: 1 / 0
                6 frame index follows the previous one of the channel: 1 / 0
                7 previous frame offsets point to the latest frame of their channel: 0 / 0
                8 time does not decrease within the channel: 1 / 0
                9 depth lies between the range limits: 2 / 0
              Channel 0 (2D): 2 frames
                Frame index: first 0, last 1, 0 gaps
                Header sizes: 168 bytes x 2
                Frame versions: 10 x 2
                Range limits: 1 distinct pair, 0 frames without a valid pair
                Frames with a valid position: 2 of 2 (100.0%)
                Checks (passed / failed): 1 2/0, 2 2/0, 3 2/0, 5 1/0, 6 1/0, 7 0/0, 8 1/0, 9 2/0
              Channel 7 (Unknown(7)): 1 frame
                Frame index: none
                Header sizes: 128 bytes x 1
                Range limits: 0 distinct pairs, 1 frame without a valid pair
                Frames with a valid position: 0 of 1 (0.0%)
                Checks (passed / failed): 1 1/0, 2 1/0, 3 1/0, 5 0/0, 6 0/0, 7 0/0, 8 0/0, 9 0/0

            Not shown unless asked for: file names, depths, the list of range-limit pairs.

            """.ReplaceLineEndings("\n");
        Assert.Equal(expected, text);
    }

    [Fact]
    public void TheDefaultReportHoldsNoFileNameNoDepthAndNoRangeLimitPair()
    {
        var text = Default(new FileReport(SecretPath, Sample()));

        Assert.DoesNotContain("secret", text);
        Assert.DoesNotContain("adam", text);
        Assert.DoesNotContain("2025", text);
        Assert.DoesNotContain("6.10", text);
        Assert.DoesNotContain("30.48", text);
        Assert.DoesNotContain("Depth", text);
    }

    [Fact]
    public void WithDetailsTheReportAddsTheFileNameTheDepthRangeAndThePairs()
    {
        var text = Report(new ReportOptions(IncludeDetails: true), new FileReport(SecretPath, Sample()));

        Assert.Contains($"File 1 of 1: 546 bytes, name: {SecretPath}\n", text);
        Assert.Contains("    Depth (m): min 6.10, max 6.10\n", text);
        Assert.Contains("    Range limit pairs (m, upper to lower): 0.00 to 30.48 x 2\n", text);
        Assert.Contains("    Depth (m): none\n", text);
        Assert.DoesNotContain("Not shown unless asked for", text);
    }

    [Fact]
    public void FilesAreNumberedInTheOrderGiven()
    {
        var text = Default(
            new FileReport("a", Sample()),
            new FileReport("b", null, FileProblem.NotFound),
            new FileReport("c", Sample()));

        Assert.StartsWith("Inventory of 3 files\n", text);
        Assert.Contains("File 1 of 3: 546 bytes\n", text);
        Assert.Contains("File 2 of 3: could not be read: file not found\n", text);
        Assert.Contains("File 3 of 3: 546 bytes\n", text);
    }

    [Theory]
    [InlineData(ChainBreakReason.TruncatedTail, "Chain: breaks at byte 408, 30 bytes left (fewer bytes left than a frame header needs)")]
    [InlineData(ChainBreakReason.OffsetMismatch, "Chain: breaks at byte 408, 30 bytes left (offset field is not the position)")]
    [InlineData(ChainBreakReason.InvalidFrameSize, "Chain: breaks at byte 408, 30 bytes left (frame size is below the smallest header or past the end of the file)")]
    public void ABrokenChainIsReportedWithItsPositionBytesLeftAndReason(ChainBreakReason reason, string line)
    {
        var inventory = Inventory(new ChainBreak(408, 30, reason), 438, Frame(8), Frame(208));

        var text = Default(new FileReport("f", inventory));

        Assert.Contains($"  {line}\n", text);
        Assert.Contains("4 chain ends exactly at the end of the file: 0 / 1; first failing at bytes 408\n", text);
    }

    [Fact]
    public void FailingPositionsAreListedAndTheOnesLeftOutCounted()
    {
        var frames = Enumerable.Range(0, 13).Select(i => Frame(8 + (i * 200L), 0, 150)).ToArray();
        var inventory = Inventory(null, 8 + (13 * 200), frames);

        var text = Default(new FileReport("f", inventory));

        Assert.Contains("3 header size is as expected: 0 / 13; first failing at bytes 8, 208, 408, 608, 808, 1008, 1208, 1408, 1608, 1808 (3 more not listed)\n", text);
    }

    [Fact]
    public void PointersCountedApartAreShown()
    {
        var inventory = Inventory(
            null,
            600,
            Frame(8, 0) with { PreviousFrameOffsets = [0, 0, 0, 0, 0, 0] });

        var text = Default(new FileReport("f", inventory));

        Assert.Contains("7 previous frame offsets point to the latest frame of their channel: 0 / 0, 4 counted apart\n", text);
    }

    [Fact]
    public void ATimeCounterIsAbsentWhenNoFrameHasOneAndNegativeValuesAreCounted()
    {
        var none = Default(new FileReport("f", Inventory(null, 208, Frame(8))));
        var negative = Default(new FileReport("f", Inventory(null, 408, Frame(8) with { TimeRaw = 0xFFFFFFF0 }, Frame(208) with { TimeRaw = 5 })));

        Assert.Contains("  Time counter: no frame has one\n", none);
        Assert.Contains("  Time counter: first 4294967280 (signed -16), last 5 (signed 5), 1 frame below zero as signed\n", negative);
    }

    [Fact]
    public void RangeLimitPairsBeyondTheCapAreReportedAsNotTracked()
    {
        var total = InventoryBuilder.MaxRangeLimitPairs + 3;
        var frames = Enumerable.Range(0, total)
            .Select(i => Frame(8 + (i * 200L)) with { UpperLimitFeet = 0f, LowerLimitFeet = 100f + i })
            .ToArray();

        var text = Default(new FileReport("f", Inventory(null, 8 + (total * 200L), frames)));

        Assert.Contains("    Range limits: 4096 distinct pairs, 0 frames without a valid pair, 3 frames beyond the cap not tracked\n", text);
    }

    [Theory]
    [InlineData(FileProblem.NotFound, "File 1 of 1: could not be read: file not found")]
    [InlineData(FileProblem.AccessDenied, "File 1 of 1: could not be read: access denied")]
    [InlineData(FileProblem.ReadFailed, "File 1 of 1: could not be read: read error")]
    public void AFileThatCannotBeReadIsReportedWithTheReasonAndWithoutItsPath(FileProblem problem, string line)
    {
        var text = Default(new FileReport(SecretPath, null, problem));

        Assert.Contains($"{line}\n", text);
        Assert.DoesNotContain("secret", text);
    }

    [Fact]
    public void AFileThatCannotBeReadShowsItsNameWithDetails()
    {
        var text = Report(new ReportOptions(true), new FileReport(SecretPath, null, FileProblem.NotFound));

        Assert.Contains($"File 1 of 1: could not be read: file not found, name: {SecretPath}\n", text);
    }

    [Fact]
    public void AFileShorterThanTheHeaderAndAFileOfAnotherFormatAreReportedAsNotRead()
    {
        var text = Default(
            new FileReport("a", null, FileProblem.TooShortForHeader, Size: 5),
            new FileReport("b", null, FileProblem.FormatNotRead, Size: 1500, Header: new FileHeader(1, 0, 0, 0)),
            new FileReport("c", null, FileProblem.FormatNotRead, Size: 20, Header: new FileHeader(9, 0, 0, 0)));

        Assert.Contains("File 1 of 3: 5 bytes, not read: shorter than the 8-byte file header\n", text);
        Assert.Contains("File 2 of 3: 1500 bytes, not read: file header format code 1 (SLG), not read further\n", text);
        Assert.Contains("File 3 of 3: 20 bytes, not read: file header format code 9 (unknown), not read further\n", text);
    }

    [Fact]
    public void NumbersAreWrittenTheSameInEveryCulture()
    {
        var before = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("sv-SE");
            var text = Report(new ReportOptions(true), new FileReport("f", Sample()));

            Assert.Contains("(100.0%)", text);
            Assert.Contains("min 6.10, max 6.10", text);
            Assert.Contains("0.00 to 30.48 x 2", text);
        }
        finally
        {
            CultureInfo.CurrentCulture = before;
        }
    }

    [Fact]
    public void RefusesMissingArguments()
    {
        Assert.Throws<ArgumentNullException>(() => InventoryTextReport.Write(null!, [], new ReportOptions()));
        Assert.Throws<ArgumentNullException>(() => InventoryTextReport.Write(new StringWriter(), null!, new ReportOptions()));
        Assert.Throws<ArgumentNullException>(() => InventoryTextReport.Write(new StringWriter(), [], null!));
    }
}
