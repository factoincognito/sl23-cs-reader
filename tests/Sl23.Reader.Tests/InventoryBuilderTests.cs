namespace Sl23.Reader.Tests;

public class InventoryBuilderTests
{
    private static readonly FileHeader Sl3Header = new(3, 0, 0, 0);
    private static readonly FileHeader Sl2Header = new(2, 0, 0, 0);

    private static FrameRecord Frame(
        long position = 8,
        ushort channel = 0,
        long headerSize = 168,
        int frameSize = 200,
        FileFormat format = FileFormat.Sl3) => new()
        {
            Format = format,
            Position = position,
            OffsetField = unchecked((uint)position),
            HeaderSize = headerSize,
            FrameSize = frameSize,
            EchoSize = frameSize - headerSize,
            ChannelId = channel,
        };

    private static FileInventory Run(FileHeader header, long size, ChainBreak? chainBreak, params FrameRecord[] frames)
    {
        var builder = new InventoryBuilder(header, size);
        foreach (var frame in frames)
        {
            builder.Add(frame);
        }

        return builder.Complete(chainBreak);
    }

    private static FileInventory Sl3(params FrameRecord[] frames) => Run(Sl3Header, 100_000, null, frames);

    private static ChannelInventory Channel(FileInventory inventory, ushort id) => inventory.Channels.Single(c => c.ChannelId == id);

    private static void AssertCheck(CheckResult check, long passed, long failed, long countedApart = 0)
    {
        Assert.Equal(passed, check.Passed);
        Assert.Equal(failed, check.Failed);
        Assert.Equal(countedApart, check.CountedApart);
    }

    [Fact]
    public void NoFramesGivesAnEmptyInventoryAndAChainThatEndedAtTheEnd()
    {
        var inventory = Run(Sl3Header, 8, null);

        Assert.Equal(Sl3Header, inventory.Header);
        Assert.Equal(8, inventory.FileSize);
        Assert.Equal(0, inventory.FrameCount);
        Assert.Empty(inventory.Layouts);
        Assert.Empty(inventory.Channels);
        Assert.Null(inventory.Break);
        Assert.Null(inventory.Time);
        Assert.Equal([1, 2, 3, 4, 5, 6, 7, 8, 9], inventory.Checks.Keys.Order());
        AssertCheck(inventory.Checks[1], 0, 0);
        AssertCheck(inventory.Checks[4], 1, 0);
        AssertCheck(inventory.Checks[9], 0, 0);
    }

    [Fact]
    public void CountsFramesLayoutsAndChannels()
    {
        var inventory = Sl3(
            Frame(8, channel: 0, headerSize: 168),
            Frame(208, channel: 7, headerSize: 128, frameSize: 138),
            Frame(346, channel: 0, headerSize: 184),
            Frame(546, channel: 5, headerSize: 168),
            Frame(746, channel: 0, headerSize: 168));

        Assert.Equal(5, inventory.FrameCount);
        Assert.Equal(3, inventory.Layouts[168]);
        Assert.Equal(1, inventory.Layouts[128]);
        Assert.Equal(1, inventory.Layouts[184]);
        Assert.Equal([0, 5, 7], inventory.Channels.Select(c => (int)c.ChannelId));
        var channel0 = Channel(inventory, 0);
        Assert.Equal("2D", channel0.Label);
        Assert.Equal(3, channel0.FrameCount);
        Assert.Equal(2, channel0.HeaderSizes[168]);
        Assert.Equal(1, channel0.HeaderSizes[184]);
        Assert.Equal("Unknown(7)", Channel(inventory, 7).Label);
        Assert.Equal(1, Channel(inventory, 5).FrameCount);
    }

    [Fact]
    public void ChecksOneAndTwoPassForEveryFrameReadAndEveryChannel()
    {
        var inventory = Sl3(Frame(8, channel: 0), Frame(208, channel: 0), Frame(408, channel: 2));

        AssertCheck(inventory.Checks[1], 3, 0);
        AssertCheck(inventory.Checks[2], 3, 0);
        AssertCheck(Channel(inventory, 0).Checks[1], 2, 0);
        AssertCheck(Channel(inventory, 2).Checks[2], 1, 0);
        Assert.DoesNotContain(4, Channel(inventory, 0).Checks.Keys);
    }

    [Theory]
    [InlineData(ChainBreakReason.OffsetMismatch, 1, 0)]
    [InlineData(ChainBreakReason.InvalidFrameSize, 0, 1)]
    [InlineData(ChainBreakReason.TruncatedTail, 0, 0)]
    public void ABreakFailsTheCheckItBelongsToAndCheckFour(ChainBreakReason reason, int failedOne, int failedTwo)
    {
        var chainBreak = new ChainBreak(408, 50, reason);

        var inventory = Run(Sl3Header, 458, chainBreak, Frame(8), Frame(208));

        Assert.Equal(chainBreak, inventory.Break);
        AssertCheck(inventory.Checks[1], 2, failedOne);
        AssertCheck(inventory.Checks[2], 2, failedTwo);
        AssertCheck(inventory.Checks[4], 0, 1);
        Assert.Equal([408L], inventory.Checks[4].FirstFailingPositions);
        if (failedOne == 1)
        {
            Assert.Equal([408L], inventory.Checks[1].FirstFailingPositions);
        }

        if (failedTwo == 1)
        {
            Assert.Equal([408L], inventory.Checks[2].FirstFailingPositions);
        }
    }

    [Theory]
    [InlineData(2, 144, 144, true)]
    [InlineData(2, 150, 150, false)]
    [InlineData(2, 168, 168, false)]
    public void CheckThreeForSl2IsAHeaderSizeOf144(int format, int header, int size, bool holds)
    {
        var frame = Frame(8, 0, header, size + 10, format == 2 ? FileFormat.Sl2 : FileFormat.Sl3);

        var inventory = Run(Sl2Header, 1000, null, frame);

        AssertCheck(inventory.Checks[3], holds ? 1 : 0, holds ? 0 : 1);
        Assert.Equal(holds ? [] : [8L], inventory.Checks[3].FirstFailingPositions);
    }

    [Theory]
    [InlineData(0, 168, true)]
    [InlineData(0, 184, true)]
    [InlineData(2, 168, true)]
    [InlineData(5, 184, true)]
    [InlineData(7, 128, true)]
    [InlineData(8, 128, true)]
    [InlineData(0, 128, false)]
    [InlineData(2, 128, false)]
    [InlineData(7, 168, false)]
    [InlineData(8, 184, false)]
    [InlineData(0, 150, false)]
    [InlineData(7, 150, false)]
    [InlineData(0, -5, false)]
    public void CheckThreeForSl3Is128OnChannels7And8AndA168Or184HeaderOnTheOthers(int channel, int header, bool holds)
    {
        var inventory = Sl3(Frame(8, (ushort)channel, header, 300));

        AssertCheck(inventory.Checks[3], holds ? 1 : 0, holds ? 0 : 1);
        AssertCheck(Channel(inventory, (ushort)channel).Checks[3], holds ? 1 : 0, holds ? 0 : 1);
    }

    [Fact]
    public void KeepsOnlyTheFirstFailingPositionsAndCountsTheRest()
    {
        var frames = Enumerable.Range(0, 13).Select(i => Frame(8 + (i * 200), 0, 150, 200)).ToArray();

        var check = Sl3(frames).Checks[3];

        Assert.Equal(13, check.Failed);
        Assert.Equal(InventoryBuilder.MaxFailingPositions, check.FirstFailingPositions.Count);
        Assert.Equal(8L, check.FirstFailingPositions[0]);
        Assert.Equal(8L + (9 * 200), check.FirstFailingPositions[^1]);
        Assert.Equal(3, check.FailuresLeftOut);
    }

    [Fact]
    public void CheckFiveComparesThePreviousFrameSizeWithTheFrameBeforeInTheFile()
    {
        var inventory = Sl3(
            Frame(8, 0, frameSize: 200) with { PreviousFrameSize = 999 },
            Frame(208, 2, frameSize: 300) with { PreviousFrameSize = 200 },
            Frame(508, 0, frameSize: 200) with { PreviousFrameSize = 300 },
            Frame(708, 0, frameSize: 200) with { PreviousFrameSize = 201 });

        var check = inventory.Checks[5];

        AssertCheck(check, 2, 1);
        Assert.Equal([708L], check.FirstFailingPositions);
        AssertCheck(Channel(inventory, 0).Checks[5], 1, 1);
        AssertCheck(Channel(inventory, 2).Checks[5], 1, 0);
    }

    [Fact]
    public void CheckFiveSkipsAReducedFrameButUsesItsSizeForTheNextFrame()
    {
        var inventory = Sl3(
            Frame(8, 0, frameSize: 200),
            Frame(208, 7, headerSize: 128, frameSize: 138),
            Frame(346, 0, frameSize: 200) with { PreviousFrameSize = 138 });

        AssertCheck(inventory.Checks[5], 1, 0);
    }

    [Fact]
    public void CheckSixComparesTheFrameIndexWithThePreviousOneOfTheSameChannel()
    {
        var inventory = Sl3(
            Frame(8, 0) with { FrameIndex = 10 },
            Frame(208, 2) with { FrameIndex = 500 },
            Frame(408, 0) with { FrameIndex = 11 },
            Frame(608, 2) with { FrameIndex = 501 },
            Frame(808, 0) with { FrameIndex = 14 },
            Frame(1008, 2) with { FrameIndex = 502 });

        AssertCheck(inventory.Checks[6], 3, 1);
        Assert.Equal([808L], inventory.Checks[6].FirstFailingPositions);
        var channel0 = Channel(inventory, 0);
        AssertCheck(channel0.Checks[6], 1, 1);
        Assert.Equal(10u, channel0.FirstFrameIndex);
        Assert.Equal(14u, channel0.LastFrameIndex);
        AssertCheck(Channel(inventory, 2).Checks[6], 2, 0);
        Assert.Equal(500u, Channel(inventory, 2).FirstFrameIndex);
        Assert.Equal(502u, Channel(inventory, 2).LastFrameIndex);
    }

    [Fact]
    public void CheckSixSkipsFramesWithoutAnIndexAndTheIndexWrapsAroundAtTheLargestValue()
    {
        var inventory = Sl3(
            Frame(8, 7, headerSize: 128, frameSize: 138),
            Frame(146, 0) with { FrameIndex = uint.MaxValue },
            Frame(346, 0) with { FrameIndex = 0 });

        AssertCheck(inventory.Checks[6], 1, 0);
        Assert.Null(Channel(inventory, 7).FirstFrameIndex);
        Assert.Null(Channel(inventory, 7).LastFrameIndex);
    }

    [Fact]
    public void CheckSevenComparesEachPointerWithTheLatestEarlierFrameOfItsChannel()
    {
        uint[] Pointers(uint p0, uint p1, uint p2, uint p3 = 0, uint p4 = 0) => [p0, p1, p2, p3, p4, 0];
        var inventory = Sl3(
            Frame(8, 0) with { PreviousFrameOffsets = Pointers(0, 0, 0) },
            Frame(208, 2) with { PreviousFrameOffsets = Pointers(8, 0, 208) },
            Frame(408, 0) with { PreviousFrameOffsets = Pointers(0, 0, 208) },
            Frame(608, 2) with { PreviousFrameOffsets = Pointers(408, 0, 0) },
            Frame(808, 0) with { PreviousFrameOffsets = Pointers(0, 0, 999) });

        var check = inventory.Checks[7];

        AssertCheck(check, 3, 1, countedApart: 16);
        Assert.Equal([808L], check.FirstFailingPositions);
        AssertCheck(Channel(inventory, 0).Checks[7], 1, 1, 10);
        AssertCheck(Channel(inventory, 2).Checks[7], 2, 0, 6);
    }

    [Fact]
    public void CheckSevenDoesNotTestAReducedFrameAndComparesPositionsAsUnsigned32BitValues()
    {
        var inventory = Sl3(
            Frame(0x1_0000_0008, 2),
            Frame(0x1_0000_0108, 7, headerSize: 128, frameSize: 138),
            Frame(0x1_0000_0200, 0) with { PreviousFrameOffsets = [0, 0, 8, 0, 0, 0] });

        AssertCheck(inventory.Checks[7], 1, 0, countedApart: 3);
    }

    [Fact]
    public void CheckEightReadsTheTimeAsSignedAndIsPerChannel()
    {
        var inventory = Sl3(
            Frame(8, 0) with { TimeRaw = 0xFFFFFFF0 },
            Frame(208, 2) with { TimeRaw = 3 },
            Frame(408, 0) with { TimeRaw = 5 },
            Frame(608, 2) with { TimeRaw = 3 },
            Frame(808, 0) with { TimeRaw = 4 },
            Frame(1008, 2) with { TimeRaw = 0xFFFFFFF0 });

        var check = inventory.Checks[8];

        AssertCheck(check, 2, 2);
        Assert.Equal([808L, 1008L], check.FirstFailingPositions);
        AssertCheck(Channel(inventory, 0).Checks[8], 1, 1);
        AssertCheck(Channel(inventory, 2).Checks[8], 1, 1);
    }

    [Fact]
    public void TheTimeSummaryHoldsTheFirstAndLastValueInBothReadingsAndTheNegativeCount()
    {
        var inventory = Sl3(
            Frame(8, 7, headerSize: 128, frameSize: 138),
            Frame(146, 0) with { TimeRaw = 0xFFFFFFF0 },
            Frame(346, 2) with { TimeRaw = 7 },
            Frame(546, 0) with { TimeRaw = 0x80000000 },
            Frame(746, 0) with { TimeRaw = 1_000_000 });

        var time = inventory.Time!;

        Assert.Equal(0xFFFFFFF0, time.FirstRaw);
        Assert.Equal(-16, time.FirstSigned);
        Assert.Equal(1_000_000u, time.LastRaw);
        Assert.Equal(1_000_000, time.LastSigned);
        Assert.Equal(2, time.NegativeAsSigned);
    }

    [Fact]
    public void CheckNineComparesTheDepthWithTheRangeLimitsOfVerticalChannelsOnly()
    {
        var limits = Frame(8, 0) with { UpperLimitFeet = 0f, LowerLimitFeet = 100f };
        FrameRecord Depth(long position, ushort channel, float? depth) =>
            Frame(position, channel) with { UpperLimitFeet = 10f, LowerLimitFeet = 100f, DepthFeet = depth };

        var inventory = Sl3(
            limits with { DepthFeet = 50f },
            Depth(208, 0, 10f),
            Depth(408, 1, 100f),
            Depth(608, 2, 101f),
            Depth(808, 0, 9.9f),
            Depth(1008, 5, 500f),
            Depth(1208, 7, null),
            Depth(1408, 0, null),
            Frame(1608, 0) with { DepthFeet = 5f });

        var check = inventory.Checks[9];

        AssertCheck(check, 3, 2);
        Assert.Equal([608L, 808L], check.FirstFailingPositions);
        AssertCheck(Channel(inventory, 0).Checks[9], 2, 1);
        AssertCheck(Channel(inventory, 5).Checks[9], 0, 0);
    }

    [Fact]
    public void CheckNineDoesNotTestAFrameWithInvalidRangeLimits()
    {
        var inventory = Sl3(
            Frame(8, 0) with { UpperLimitFeet = 50f, LowerLimitFeet = 20f, DepthFeet = 30f },
            Frame(208, 0) with { UpperLimitFeet = float.NaN, LowerLimitFeet = 20f, DepthFeet = 30f });

        AssertCheck(inventory.Checks[9], 0, 0);
    }

    [Fact]
    public void CountsDistinctRangeLimitPairsAndFramesWithoutAValidPair()
    {
        FrameRecord Pair(long position, float upper, float lower) =>
            Frame(position, 0) with { UpperLimitFeet = upper, LowerLimitFeet = lower };

        var inventory = Sl3(
            Pair(8, 0f, 100f),
            Pair(208, 0f, 100f),
            Pair(408, 0f, 120f),
            Pair(608, 50f, 20f),
            Frame(808, 0),
            Frame(1008, 7, headerSize: 128, frameSize: 138));

        var channel = Channel(inventory, 0);

        Assert.Equal(2, channel.DistinctRangeLimitPairs);
        Assert.Equal(2, channel.RangeLimitPairs[(0, Units.FeetToMetres(100f))]);
        Assert.Equal(1, channel.RangeLimitPairs[(0, Units.FeetToMetres(120f))]);
        Assert.Equal(2, channel.FramesWithoutValidRangeLimits);
        Assert.Equal(0, channel.FramesWithUntrackedRangeLimits);
        Assert.Equal(1, Channel(inventory, 7).FramesWithoutValidRangeLimits);
    }

    [Fact]
    public void StopsTrackingNewRangeLimitPairsAtTheCapAndCountsTheFramesLeftOut()
    {
        var total = InventoryBuilder.MaxRangeLimitPairs + 5;
        var frames = Enumerable.Range(0, total)
            .Select(i => Frame(8 + (i * 200L), 0) with { UpperLimitFeet = 0f, LowerLimitFeet = 100f + i })
            .Append(Frame(8 + (total * 200L), 0) with { UpperLimitFeet = 0f, LowerLimitFeet = 100f })
            .ToArray();

        var channel = Channel(Sl3(frames), 0);

        Assert.Equal(InventoryBuilder.MaxRangeLimitPairs, channel.DistinctRangeLimitPairs);
        Assert.Equal(InventoryBuilder.MaxRangeLimitPairs, channel.RangeLimitPairs.Count);
        Assert.Equal(5, channel.FramesWithUntrackedRangeLimits);
        Assert.Equal(2, channel.RangeLimitPairs[(0, Units.FeetToMetres(100f))]);
    }

    [Fact]
    public void CountsFramesWithAValidPositionAndTracksTheDepthRange()
    {
        var inventory = Sl3(
            Frame(8, 0) with { Flags = 0x0212, X = 100, Y = 200, DepthFeet = 30f },
            Frame(208, 0) with { Flags = 0x0200, X = 100, Y = 200, DepthFeet = 10f },
            Frame(408, 0) with { Flags = 0x0010, X = 100, Y = 200, DepthFeet = 50f },
            Frame(608, 0),
            Frame(808, 2));

        var channel = Channel(inventory, 0);

        Assert.Equal(2, channel.FramesWithValidPosition);
        Assert.Equal(Units.FeetToMetres(10f), channel.DepthMinMetres);
        Assert.Equal(Units.FeetToMetres(50f), channel.DepthMaxMetres);
        Assert.Equal(0, Channel(inventory, 2).FramesWithValidPosition);
        Assert.Null(Channel(inventory, 2).DepthMinMetres);
        Assert.Null(Channel(inventory, 2).DepthMaxMetres);
    }

    [Fact]
    public void CountsTheFrameVersionsOfAChannel()
    {
        var inventory = Sl3(
            Frame(8, 0) with { FrameVersion = 10 },
            Frame(208, 0) with { FrameVersion = 14 },
            Frame(408, 0) with { FrameVersion = 10 },
            Frame(608, 7, headerSize: 128, frameSize: 138));

        Assert.Equal(2, Channel(inventory, 0).FrameVersions[10]);
        Assert.Equal(1, Channel(inventory, 0).FrameVersions[14]);
        Assert.Empty(Channel(inventory, 7).FrameVersions);
    }

    [Fact]
    public void TalliesAFileReadByTheSl3Reader()
    {
        var file = Sl3File.Build(
            new Sl3Frame { Channel = 0, Echo = new byte[10], FrameIndex = 0, PreviousFrameSize = 0, Upper = 0f, Lower = 100f, Depth = 20f, Time = 100 },
            new Sl3Frame { Channel = 7, HeaderSize = 128, Echo = new byte[10] },
            new Sl3Frame { Channel = 0, Echo = new byte[10], FrameIndex = 1, PreviousFrameSize = 138, Upper = 0f, Lower = 100f, Depth = 20f, Time = 101 });
        var reader = new Sl3FrameReader(new MemoryStream(file));
        var builder = new InventoryBuilder(Sl3Header, file.Length);
        while (reader.ReadNext() is { } frame)
        {
            builder.Add(frame);
        }

        var inventory = builder.Complete(reader.Break);

        Assert.Equal(3, inventory.FrameCount);
        Assert.Null(inventory.Break);
        AssertCheck(inventory.Checks[1], 3, 0);
        AssertCheck(inventory.Checks[3], 3, 0);
        AssertCheck(inventory.Checks[4], 1, 0);
        AssertCheck(inventory.Checks[5], 1, 0);
        AssertCheck(inventory.Checks[6], 1, 0);
        AssertCheck(inventory.Checks[8], 1, 0);
        AssertCheck(inventory.Checks[9], 2, 0);
        Assert.Equal(100u, inventory.Time!.FirstRaw);
        Assert.Equal(101u, inventory.Time.LastRaw);
    }

    [Fact]
    public void RefusesBadUse()
    {
        var builder = new InventoryBuilder(Sl3Header, 100);
        Assert.Throws<ArgumentNullException>(() => builder.Add(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => new InventoryBuilder(Sl3Header, -1));
        _ = builder.Complete(null);
        Assert.Throws<InvalidOperationException>(() => builder.Add(Frame()));
        Assert.Throws<InvalidOperationException>(() => builder.Complete(null));
    }
}
