namespace Sl23.Reader;

/// <summary>The label of a channel id (design section 6.6).</summary>
public static class ChannelLabels
{
    /// <summary>Returns the label of a channel: "2D", "Secondary", "DownScan", "SideScanLeft", "SideScanRight", "SideScan", or "Unknown(n)" for any other id.</summary>
    /// <param name="channelId">The channel id from the frame header.</param>
    /// <returns>The label, as the analyser's reports and fomo-f's decoder name it.</returns>
    public static string For(ushort channelId) => throw new NotImplementedException();
}
