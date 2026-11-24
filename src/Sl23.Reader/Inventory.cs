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

    /// <summary>Initializes a new instance of the <see cref="InventoryBuilder"/> class.</summary>
    /// <param name="header">The file header.</param>
    /// <param name="fileSize">The file size in bytes.</param>
    public InventoryBuilder(FileHeader header, long fileSize) => throw new NotImplementedException();

    /// <summary>Adds the next frame of the file; frames must come in file order.</summary>
    /// <param name="frame">The frame.</param>
    public void Add(FrameRecord frame) => throw new NotImplementedException();

    /// <summary>Ends the file and returns its inventory.</summary>
    /// <param name="chainBreak">Where the chain broke, or <see langword="null"/> when it ended at the end of the file.</param>
    /// <returns>The inventory.</returns>
    public FileInventory Complete(ChainBreak? chainBreak) => throw new NotImplementedException();
}
