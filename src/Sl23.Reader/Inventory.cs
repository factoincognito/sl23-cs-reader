namespace Sl23.Reader;

/// <summary>The outcome of one health check (design section 6.3) over a file or a channel.</summary>
/// <param name="Passed">How many times the check was applied and held.</param>
/// <param name="Failed">How many times it was applied and did not hold.</param>
/// <param name="FirstFailingPositions">The file positions of the first failures, at most <see cref="InventoryBuilder.MaxFailingPositions"/>.</param>
/// <param name="FailuresLeftOut">How many failures are not in <paramref name="FirstFailingPositions"/>.</param>
/// <param name="CountedApart">Applications that could not be judged and are counted apart (check 7: a pointer to a channel not yet seen).</param>
public sealed record CheckResult(long Passed, long Failed, IReadOnlyList<long> FirstFailingPositions, long FailuresLeftOut, long CountedApart);

/// <summary>The first and last value of the time field of a file, in file order, and how many frames read below zero as signed.</summary>
/// <param name="FirstRaw">The first value, as stored (u32).</param>
/// <param name="LastRaw">The last value, as stored (u32).</param>
/// <param name="NegativeAsSigned">The number of frames whose value reads below zero as a signed i32.</param>
public sealed record TimeSummary(uint FirstRaw, uint LastRaw, long NegativeAsSigned)
{
    /// <summary>Gets the first value read as a signed i32.</summary>
    public int FirstSigned => Units.TimeAsSigned(FirstRaw);

    /// <summary>Gets the last value read as a signed i32.</summary>
    public int LastSigned => Units.TimeAsSigned(LastRaw);
}

/// <summary>What the inventory holds for one channel of a file (design section 7.2).</summary>
/// <param name="ChannelId">The channel id.</param>
/// <param name="Label">The channel's label.</param>
/// <param name="FrameCount">The number of frames of the channel.</param>
/// <param name="FirstFrameIndex">The frame index of the first frame that has one.</param>
/// <param name="LastFrameIndex">The frame index of the last frame that has one.</param>
/// <param name="HeaderSizes">The header sizes found and how many frames have each.</param>
/// <param name="FrameVersions">The frame versions found and how many frames have each.</param>
/// <param name="DistinctRangeLimitPairs">The number of distinct pairs of range limits (metres) seen, up to <see cref="InventoryBuilder.MaxRangeLimitPairs"/>.</param>
/// <param name="RangeLimitPairs">The pairs and how many frames have each; frames whose pair arrived after the cap are in <paramref name="FramesWithUntrackedRangeLimits"/>.</param>
/// <param name="FramesWithUntrackedRangeLimits">Frames with a valid pair that is not tracked because the cap was reached.</param>
/// <param name="FramesWithoutValidRangeLimits">Frames with no valid pair of range limits.</param>
/// <param name="FramesWithValidPosition">Frames with the position flag set and a position.</param>
/// <param name="DepthMinMetres">The smallest depth in metres, when any frame has a depth.</param>
/// <param name="DepthMaxMetres">The largest depth in metres, when any frame has a depth.</param>
/// <param name="Checks">The health checks by number (1, 2, 3, 5, 6, 7, 8, 9) over the channel's frames.</param>
public sealed record ChannelInventory(
    ushort ChannelId,
    string Label,
    long FrameCount,
    uint? FirstFrameIndex,
    uint? LastFrameIndex,
    IReadOnlyDictionary<long, long> HeaderSizes,
    IReadOnlyDictionary<uint, long> FrameVersions,
    int DistinctRangeLimitPairs,
    IReadOnlyDictionary<(double Upper, double Lower), long> RangeLimitPairs,
    long FramesWithUntrackedRangeLimits,
    long FramesWithoutValidRangeLimits,
    long FramesWithValidPosition,
    double? DepthMinMetres,
    double? DepthMaxMetres,
    IReadOnlyDictionary<int, CheckResult> Checks);

/// <summary>What the inventory holds for one file (design section 7.2).</summary>
/// <param name="Header">The file header.</param>
/// <param name="FileSize">The file size in bytes.</param>
/// <param name="FrameCount">The number of frames read.</param>
/// <param name="Layouts">The header sizes found and how many frames have each.</param>
/// <param name="Break">Where the chain broke, or <see langword="null"/> when it ended exactly at the end of the file.</param>
/// <param name="Time">The time field's first and last value, or <see langword="null"/> when no frame has one.</param>
/// <param name="Checks">The health checks by number (1 to 9) over the whole file.</param>
/// <param name="Channels">The channels in order of channel id.</param>
public sealed record FileInventory(
    FileHeader Header,
    long FileSize,
    long FrameCount,
    IReadOnlyDictionary<long, long> Layouts,
    ChainBreak? Break,
    TimeSummary? Time,
    IReadOnlyDictionary<int, CheckResult> Checks,
    IReadOnlyList<ChannelInventory> Channels);

/// <summary>
/// Tallies the frames of one file in file order: counts, layouts, channels and the health checks of design
/// section 6.3. It holds counts and a bounded number of positions and range-limit pairs, never the frames.
/// </summary>
public sealed class InventoryBuilder
{
    /// <summary>The most failing positions kept for one check.</summary>
    public const int MaxFailingPositions = 10;

    /// <summary>The most distinct pairs of range limits kept for one channel.</summary>
    public const int MaxRangeLimitPairs = 4096;

    private const int LastPointerChannel = 4;

    private readonly FileHeader header;
    private readonly long fileSize;
    private readonly SortedDictionary<long, long> layouts = [];
    private readonly SortedDictionary<ushort, ChannelState> channels = [];
    private readonly Tally[] fileChecks = Enumerable.Range(0, 10).Select(_ => new Tally()).ToArray();
    private long frameCount;
    private int? previousFrameSize;
    private uint? firstTime;
    private uint lastTime;
    private long negativeTimes;
    private bool completed;

    /// <summary>Initializes a new instance of the <see cref="InventoryBuilder"/> class.</summary>
    /// <param name="header">The file header.</param>
    /// <param name="fileSize">The file size in bytes.</param>
    public InventoryBuilder(FileHeader header, long fileSize)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(fileSize);
        this.header = header;
        this.fileSize = fileSize;
    }

    /// <summary>Adds the next frame of the file; frames must come in file order.</summary>
    /// <param name="frame">The frame.</param>
    public void Add(FrameRecord frame)
    {
        ArgumentNullException.ThrowIfNull(frame);
        if (completed)
        {
            throw new InvalidOperationException("The inventory is complete.");
        }

        frameCount++;
        layouts[frame.HeaderSize] = layouts.GetValueOrDefault(frame.HeaderSize) + 1;
        if (!channels.TryGetValue(frame.ChannelId, out var channel))
        {
            channel = new ChannelState();
            channels[frame.ChannelId] = channel;
        }

        channel.FrameCount++;
        channel.HeaderSizes[frame.HeaderSize] = channel.HeaderSizes.GetValueOrDefault(frame.HeaderSize) + 1;
        Record(1, channel, true, frame.Position);
        Record(2, channel, true, frame.Position);
        Record(3, channel, HasExpectedHeaderSize(frame), frame.Position);

        if (frame.PreviousFrameSize is { } stored && previousFrameSize is { } before)
        {
            Record(5, channel, stored == before, frame.Position);
        }

        previousFrameSize = frame.FrameSize;
        TallyFrameIndex(frame, channel);
        TallyPreviousOffsets(frame, channel);
        TallyTime(frame, channel);
        TallyRange(frame, channel);
        if (frame.Longitude is not null && frame.Latitude is not null)
        {
            channel.FramesWithValidPosition++;
        }

        if (frame.FrameVersion is { } version)
        {
            channel.FrameVersions[version] = channel.FrameVersions.GetValueOrDefault(version) + 1;
        }

        channel.LastPosition = frame.Position;
    }

    /// <summary>Ends the file and returns its inventory.</summary>
    /// <param name="chainBreak">Where the chain broke, or <see langword="null"/> when it ended at the end of the file.</param>
    /// <returns>The inventory.</returns>
    public FileInventory Complete(ChainBreak? chainBreak)
    {
        if (completed)
        {
            throw new InvalidOperationException("The inventory is complete.");
        }

        completed = true;
        if (chainBreak is null)
        {
            fileChecks[4].Pass();
        }
        else
        {
            fileChecks[4].Fail(chainBreak.Position);
            if (chainBreak.Reason == ChainBreakReason.OffsetMismatch)
            {
                fileChecks[1].Fail(chainBreak.Position);
            }
            else if (chainBreak.Reason == ChainBreakReason.InvalidFrameSize)
            {
                fileChecks[2].Fail(chainBreak.Position);
            }
        }

        TimeSummary? time = firstTime is { } first ? new TimeSummary(first, lastTime, negativeTimes) : null;
        var checks = Enumerable.Range(1, 9).ToDictionary(n => n, n => fileChecks[n].ToResult());
        var channelResults = channels.Select(c => c.Value.ToInventory(c.Key)).ToList();
        return new FileInventory(header, fileSize, frameCount, layouts, chainBreak, time, checks, channelResults);
    }

    private static bool HasExpectedHeaderSize(FrameRecord frame)
    {
        if (frame.Format == FileFormat.Sl2)
        {
            return frame.HeaderSize == Sl2FrameReader.HeaderSize;
        }

        var reduced = frame.ChannelId is 7 or 8;
        return reduced
            ? frame.HeaderSize == Sl3FrameReader.SmallestHeaderSize
            : frame.HeaderSize is 168 or 184;
    }

    private static bool IsVertical(ushort channelId) => channelId is 0 or 1 or 2;

    private void Record(int check, ChannelState channel, bool holds, long position)
    {
        fileChecks[check].Add(holds, position);
        channel.Checks[check].Add(holds, position);
    }

    private void TallyFrameIndex(FrameRecord frame, ChannelState channel)
    {
        if (frame.FrameIndex is not { } index)
        {
            return;
        }

        channel.FirstFrameIndex ??= index;
        if (channel.LastFrameIndex is { } last)
        {
            Record(6, channel, index == unchecked(last + 1), frame.Position);
        }

        channel.LastFrameIndex = index;
    }

    private void TallyPreviousOffsets(FrameRecord frame, ChannelState channel)
    {
        if (frame.PreviousFrameOffsets is not { } offsets)
        {
            return;
        }

        for (var named = 0; named <= LastPointerChannel && named < offsets.Count; named++)
        {
            if (named == frame.ChannelId)
            {
                continue;
            }

            if (channels.TryGetValue((ushort)named, out var other) && other.LastPosition is { } position)
            {
                Record(7, channel, offsets[named] == unchecked((uint)position), frame.Position);
            }
            else
            {
                fileChecks[7].Apart();
                channel.Checks[7].Apart();
            }
        }
    }

    private void TallyTime(FrameRecord frame, ChannelState channel)
    {
        if (frame.TimeRaw is not { } raw)
        {
            return;
        }

        var signed = Units.TimeAsSigned(raw);
        firstTime ??= raw;
        lastTime = raw;
        if (signed < 0)
        {
            negativeTimes++;
        }

        if (channel.LastTimeSigned is { } before)
        {
            Record(8, channel, signed >= before, frame.Position);
        }

        channel.LastTimeSigned = signed;
    }

    private void TallyRange(FrameRecord frame, ChannelState channel)
    {
        var limits = frame.RangeLimitsMetres;
        if (limits is { } pair)
        {
            channel.CountRangeLimits(pair);
        }
        else
        {
            channel.FramesWithoutValidRangeLimits++;
        }

        if (frame.DepthMetres is { } depth && double.IsFinite(depth))
        {
            channel.DepthMin = channel.DepthMin is { } min ? Math.Min(min, depth) : depth;
            channel.DepthMax = channel.DepthMax is { } max ? Math.Max(max, depth) : depth;
            if (limits is { } range && IsVertical(frame.ChannelId))
            {
                Record(9, channel, depth >= range.Upper && depth <= range.Lower, frame.Position);
            }
        }
    }

    private sealed class Tally
    {
        private readonly List<long> positions = [];
        private long passed;
        private long failed;
        private long leftOut;
        private long apart;

        public void Add(bool holds, long position)
        {
            if (holds)
            {
                Pass();
            }
            else
            {
                Fail(position);
            }
        }

        public void Pass() => passed++;

        public void Apart() => apart++;

        public void Fail(long position)
        {
            failed++;
            if (positions.Count < MaxFailingPositions)
            {
                positions.Add(position);
            }
            else
            {
                leftOut++;
            }
        }

        public CheckResult ToResult() => new(passed, failed, positions.ToArray(), leftOut, apart);
    }

    private sealed class ChannelState
    {
        public long FrameCount { get; set; }

        public uint? FirstFrameIndex { get; set; }

        public uint? LastFrameIndex { get; set; }

        public long? LastPosition { get; set; }

        public int? LastTimeSigned { get; set; }

        public SortedDictionary<long, long> HeaderSizes { get; } = [];

        public SortedDictionary<uint, long> FrameVersions { get; } = [];

        public Dictionary<(double Upper, double Lower), long> RangeLimitPairs { get; } = [];

        public long FramesWithUntrackedRangeLimits { get; set; }

        public long FramesWithoutValidRangeLimits { get; set; }

        public long FramesWithValidPosition { get; set; }

        public double? DepthMin { get; set; }

        public double? DepthMax { get; set; }

        public Dictionary<int, Tally> Checks { get; } = new[] { 1, 2, 3, 5, 6, 7, 8, 9 }.ToDictionary(n => n, _ => new Tally());

        public void CountRangeLimits((double Upper, double Lower) pair)
        {
            if (RangeLimitPairs.TryGetValue(pair, out var count))
            {
                RangeLimitPairs[pair] = count + 1;
            }
            else if (RangeLimitPairs.Count < MaxRangeLimitPairs)
            {
                RangeLimitPairs[pair] = 1;
            }
            else
            {
                FramesWithUntrackedRangeLimits++;
            }
        }

        public ChannelInventory ToInventory(ushort channelId) => new(
            channelId,
            ChannelLabels.For(channelId),
            FrameCount,
            FirstFrameIndex,
            LastFrameIndex,
            HeaderSizes,
            FrameVersions,
            RangeLimitPairs.Count,
            RangeLimitPairs,
            FramesWithUntrackedRangeLimits,
            FramesWithoutValidRangeLimits,
            FramesWithValidPosition,
            DepthMin,
            DepthMax,
            Checks.ToDictionary(c => c.Key, c => c.Value.ToResult()));
    }
}
