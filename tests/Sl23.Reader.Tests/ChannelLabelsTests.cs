namespace Sl23.Reader.Tests;

public class ChannelLabelsTests
{
    [Theory]
    [InlineData(0, "2D")]
    [InlineData(1, "Secondary")]
    [InlineData(2, "DownScan")]
    [InlineData(3, "SideScanLeft")]
    [InlineData(4, "SideScanRight")]
    [InlineData(5, "SideScan")]
    public void NamesTheEstablishedChannels(int id, string label)
    {
        Assert.Equal(label, ChannelLabels.For((ushort)id));
    }

    [Theory]
    [InlineData(6, "Unknown(6)")]
    [InlineData(7, "Unknown(7)")]
    [InlineData(8, "Unknown(8)")]
    [InlineData(10, "Unknown(10)")]
    [InlineData(11, "Unknown(11)")]
    [InlineData(65535, "Unknown(65535)")]
    public void NamesAnyOtherChannelByItsId(int id, string label)
    {
        Assert.Equal(label, ChannelLabels.For((ushort)id));
    }
}
