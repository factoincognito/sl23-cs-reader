namespace Sl23.Reader.Tests;

public class FileHeaderTests
{
    private static readonly byte[] Sl2Header = [0x02, 0x00, 0x34, 0x12, 0x78, 0x56, 0xBC, 0x9A];

    [Fact]
    public void TheHeaderIsEightBytes()
    {
        Assert.Equal(8, FileHeader.Size);
    }

    [Fact]
    public void ReadsTheFourWordsLittleEndian()
    {
        var parsed = FileHeader.TryParse(Sl2Header, out var header);

        Assert.True(parsed);
        Assert.Equal((ushort)2, header.FormatCode);
        Assert.Equal((ushort)0x1234, header.Word2);
        Assert.Equal((ushort)0x5678, header.Word4);
        Assert.Equal((ushort)0x9ABC, header.Word6);
    }

    [Theory]
    [InlineData(1, FileFormat.Slg, false)]
    [InlineData(2, FileFormat.Sl2, true)]
    [InlineData(3, FileFormat.Sl3, true)]
    public void NamesTheFormatFromTheCode(int code, FileFormat format, bool readable)
    {
        var header = new FileHeader((ushort)code, 0, 0, 0);

        Assert.Equal(format, header.Format);
        Assert.Equal(readable, header.HasReadableFrames);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    [InlineData(0xFFFF)]
    public void AnyOtherCodeIsUnknownAndNotRead(int code)
    {
        var header = new FileHeader((ushort)code, 0, 0, 0);

        Assert.Equal(FileFormat.Unknown, header.Format);
        Assert.False(header.HasReadableFrames);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(7)]
    public void AFileShorterThanTheHeaderHoldsNoHeader(int length)
    {
        var parsed = FileHeader.TryParse(Sl2Header.AsSpan(0, length), out var header);

        Assert.False(parsed);
        Assert.Equal(default, header);
    }

    [Fact]
    public void ReadsOnlyTheFirstEightBytes()
    {
        byte[] longer = [.. Sl2Header, 0xFF, 0xFF, 0xFF];

        var parsed = FileHeader.TryParse(longer, out var header);

        Assert.True(parsed);
        Assert.Equal((ushort)0x9ABC, header.Word6);
    }
}
