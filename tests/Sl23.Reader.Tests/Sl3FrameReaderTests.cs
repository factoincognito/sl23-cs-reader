namespace Sl23.Reader.Tests;

public class Sl3FrameReaderTests
{
    private static readonly byte[] Echo10 = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];

    private static List<FrameRecord> ReadAll(Sl3FrameReader reader)
    {
        var frames = new List<FrameRecord>();
        while (reader.ReadNext() is { } frame)
        {
            frames.Add(frame);
        }

        return frames;
    }

    private static Sl3FrameReader Reader(byte[] file, bool header = false, bool echo = false) =>
        new(new MemoryStream(file), header, echo);

    [Fact]
    public void AFileWithOnlyTheFileHeaderHasNoFramesAndNoBreak()
    {
        var reader = Reader(Sl3File.Build());

        Assert.Null(reader.ReadNext());
        Assert.Null(reader.Break);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    public void AFileShorterThanTheFileHeaderHasNoFramesAndNoBreak(int length)
    {
        var reader = Reader(new byte[length]);

        Assert.Null(reader.ReadNext());
        Assert.Null(reader.Break);
    }

    [Theory]
    [InlineData(168)]
    [InlineData(184)]
    public void ReadsEveryFieldOfAFrameWithA168Or184ByteHeader(int headerSize)
    {
        var file = Sl3File.Build(new Sl3Frame
        {
            Channel = 5,
            HeaderSize = headerSize,
            Echo = Echo10,
            Version = 14,
            PreviousFrameSize = 77,
            FrameIndex = 123456,
            Upper = 1.5f,
            Lower = 200.25f,
            Depth = 33.5f,
            Speed = 4.75f,
            Course = 1.25f,
            X = -1234567,
            Y = 7654321,
            Flags = 0x0212,
            Time = 4_000_000_000,
            PreviousOffsets = [11, 22, 33, 44, 55, 66],
        });
        var reader = Reader(file);

        var frame = Assert.Single(ReadAll(reader));

        Assert.Equal(FileFormat.Sl3, frame.Format);
        Assert.Equal(8, frame.Position);
        Assert.Equal(8u, frame.OffsetField);
        Assert.Equal(headerSize, frame.HeaderSize);
        Assert.Equal(headerSize + 10, frame.FrameSize);
        Assert.Equal(10, frame.EchoSize);
        Assert.Equal((ushort)5, frame.ChannelId);
        Assert.Equal("SideScan", frame.ChannelLabel);
        Assert.Equal(14u, frame.FrameVersion);
        Assert.Equal(123456u, frame.FrameIndex);
        Assert.Equal((ushort)77, frame.PreviousFrameSize);
        Assert.Equal(1.5f, frame.UpperLimitFeet);
        Assert.Equal(200.25f, frame.LowerLimitFeet);
        Assert.Equal(33.5f, frame.DepthFeet);
        Assert.Equal(4.75f, frame.GpsSpeedKnots);
        Assert.Equal(1.25f, frame.CourseRadians);
        Assert.Equal(-1234567, frame.X);
        Assert.Equal(7654321, frame.Y);
        Assert.Equal((ushort)0x0212, frame.Flags);
        Assert.Equal(4_000_000_000u, frame.TimeRaw);
        Assert.Equal([11u, 22u, 33u, 44u, 55u, 66u], frame.PreviousFrameOffsets);
        Assert.Null(reader.Break);
    }

    [Fact]
    public void AFrameWithA128ByteHeaderGivesOnlyTheFramingFields()
    {
        var file = Sl3File.Build(new Sl3Frame
        {
            Channel = 7,
            HeaderSize = 128,
            Echo = Echo10,
            FrameIndex = 99,
            Depth = 12f,
            Flags = Units.PositionValidFlag,
            X = 1,
            Y = 2,
            Time = 5,
        });
        var reader = Reader(file);

        var frame = Assert.Single(ReadAll(reader));

        Assert.Equal(FileFormat.Sl3, frame.Format);
        Assert.Equal(8, frame.Position);
        Assert.Equal(8u, frame.OffsetField);
        Assert.Equal(128, frame.HeaderSize);
        Assert.Equal(138, frame.FrameSize);
        Assert.Equal(10, frame.EchoSize);
        Assert.Equal((ushort)7, frame.ChannelId);
        Assert.Equal("Unknown(7)", frame.ChannelLabel);
        Assert.Null(frame.FrameIndex);
        Assert.Null(frame.PreviousFrameSize);
        Assert.Null(frame.UpperLimitFeet);
        Assert.Null(frame.LowerLimitFeet);
        Assert.Null(frame.DepthFeet);
        Assert.Null(frame.Flags);
        Assert.Null(frame.X);
        Assert.Null(frame.Y);
        Assert.Null(frame.GpsSpeedKnots);
        Assert.Null(frame.CourseRadians);
        Assert.Null(frame.TimeRaw);
        Assert.Null(frame.PreviousFrameOffsets);
        Assert.Null(frame.FrameVersion);
        Assert.Null(frame.Longitude);
    }

    [Fact]
    public void FramesWithDifferentHeaderSizesFollowEachOtherByTheirFrameSize()
    {
        var file = Sl3File.Build(
            new Sl3Frame { HeaderSize = 168, Echo = Echo10, FrameIndex = 0 },
            new Sl3Frame { Channel = 7, HeaderSize = 128, Echo = Echo10 },
            new Sl3Frame { HeaderSize = 184, Echo = new byte[300], FrameIndex = 1 },
            new Sl3Frame { Channel = 8, HeaderSize = 128 });
        var reader = Reader(file);

        var frames = ReadAll(reader);

        Assert.Equal([8L, 186L, 324L, 808L], frames.Select(f => f.Position));
        Assert.Equal([168, 128, 184, 128], frames.Select(f => f.HeaderSize));
        Assert.Equal([(ushort)0, (ushort)7, (ushort)0, (ushort)8], frames.Select(f => f.ChannelId));
        Assert.Null(reader.Break);
    }

    [Fact]
    public void ReadingPastTheEndKeepsReturningNull()
    {
        var reader = Reader(Sl3File.Build(new Sl3Frame { Echo = Echo10 }));
        _ = ReadAll(reader);

        Assert.Null(reader.ReadNext());
        Assert.Null(reader.ReadNext());
    }

    [Fact]
    public void TheEchoIsTheLastEchoSizeBytesOfTheFrame()
    {
        var file = Sl3File.Build(new Sl3Frame { HeaderSize = 184, Echo = Echo10, FrameIndex = 5 }, new Sl3Frame { FrameIndex = 6 });

        var frames = ReadAll(Reader(file, echo: true));

        Assert.Equal(Echo10, frames[0].Echo!.Value.ToArray());
        Assert.Equal(0, frames[1].Echo!.Value.Length);
        Assert.Equal(6u, frames[1].FrameIndex);
    }

    [Fact]
    public void SkipsTheEchoUnlessAskedForIt()
    {
        var file = Sl3File.Build(new Sl3Frame { Echo = Echo10 }, new Sl3Frame { FrameIndex = 9 });

        var frames = ReadAll(Reader(file));

        Assert.Null(frames[0].Echo);
        Assert.Null(frames[0].HeaderBytes);
        Assert.Equal(9u, frames[1].FrameIndex);
    }

    [Theory]
    [InlineData(168)]
    [InlineData(184)]
    [InlineData(128)]
    public void CarriesTheHeaderBytesWhenAskedFor(int headerSize)
    {
        var file = Sl3File.Build(new Sl3Frame { HeaderSize = headerSize, Echo = Echo10, Channel = 2 });

        var frame = Assert.Single(ReadAll(Reader(file, header: true)));

        Assert.Equal(headerSize, frame.HeaderBytes!.Value.Length);
        Assert.Equal(2, frame.HeaderBytes!.Value.Span[12]);
    }

    [Fact]
    public void AnOtherHeaderSizeIsAFindingAndGivesOnlyTheFramingFields()
    {
        var file = Sl3File.Build(
            new Sl3Frame { HeaderSize = 150, Echo = Echo10, FrameIndex = 1, Depth = 5f },
            new Sl3Frame { FrameIndex = 2 });
        var reader = Reader(file, header: true);

        var frames = ReadAll(reader);

        Assert.Equal(2, frames.Count);
        Assert.Equal(150, frames[0].HeaderSize);
        Assert.Equal(150, frames[0].HeaderBytes!.Value.Length);
        Assert.Null(frames[0].FrameIndex);
        Assert.Null(frames[0].DepthFeet);
        Assert.Equal(2u, frames[1].FrameIndex);
        Assert.Null(reader.Break);
    }

    [Fact]
    public void AnEchoSizeLargerThanTheFrameGivesANegativeHeaderSizeFound()
    {
        var file = Sl3File.Build(new Sl3Frame { HeaderSize = 168, EchoSize = 1000 }, new Sl3Frame { FrameIndex = 2 });
        var reader = Reader(file, header: true, echo: true);

        var frames = ReadAll(reader);

        Assert.Equal(2, frames.Count);
        Assert.Equal(168 - 1000, frames[0].HeaderSize);
        Assert.Equal(1000, frames[0].EchoSize);
        Assert.Equal(0, frames[0].HeaderBytes!.Value.Length);
        Assert.Equal(0, frames[0].Echo!.Value.Length);
        Assert.Null(frames[0].FrameIndex);
        Assert.Equal(2u, frames[1].FrameIndex);
    }

    [Fact]
    public void AnEchoSizeBeyondTheRangeOfAnIntegerIsKeptAsStored()
    {
        var file = Sl3File.Build(new Sl3Frame { HeaderSize = 184, EchoSize = uint.MaxValue });
        var reader = Reader(file, header: true, echo: true);

        var frame = Assert.Single(ReadAll(reader));

        Assert.Equal(uint.MaxValue, (uint)frame.EchoSize);
        Assert.Equal(184 - (long)uint.MaxValue, frame.HeaderSize);
        Assert.Equal(0, frame.Echo!.Value.Length);
        Assert.Null(frame.FrameIndex);
    }

    [Fact]
    public void AWrongOffsetFieldBreaksTheChainAtThatFrame()
    {
        var file = Sl3File.Build(
            new Sl3Frame { Echo = Echo10, FrameIndex = 1 },
            new Sl3Frame { OffsetField = 999, Echo = Echo10, FrameIndex = 2 },
            new Sl3Frame { FrameIndex = 3 });
        var reader = Reader(file);

        var frames = ReadAll(reader);

        var frame = Assert.Single(frames);
        Assert.Equal(1u, frame.FrameIndex);
        Assert.Equal(new ChainBreak(186, file.Length - 186, ChainBreakReason.OffsetMismatch), reader.Break);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(127)]
    [InlineData(60000)]
    public void AFrameSizeBelowTheSmallestHeaderOrPastTheEndBreaksTheChain(int frameSize)
    {
        var file = Sl3File.Build(
            new Sl3Frame { Echo = Echo10 },
            new Sl3Frame { FrameSize = (ushort)frameSize, Echo = Echo10 });
        var reader = Reader(file);

        var frames = ReadAll(reader);

        Assert.Single(frames);
        Assert.Equal(new ChainBreak(186, file.Length - 186, ChainBreakReason.InvalidFrameSize), reader.Break);
    }

    [Fact]
    public void AFrameSizeOfExactly128IsAccepted()
    {
        var file = Sl3File.Build(new Sl3Frame { Channel = 7, HeaderSize = 128 });
        var reader = Reader(file);

        var frame = Assert.Single(ReadAll(reader));

        Assert.Equal(128, frame.FrameSize);
        Assert.Null(reader.Break);
    }

    [Fact]
    public void AFrameThatEndsOneByteBeyondTheFileBreaksTheChain()
    {
        var file = Sl3File.Build(new Sl3Frame { FrameSize = 169 });
        var reader = Reader(file);

        Assert.Null(reader.ReadNext());
        Assert.Equal(new ChainBreak(8, 168, ChainBreakReason.InvalidFrameSize), reader.Break);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(30)]
    [InlineData(47)]
    public void FewerThanFortyEightBytesLeftIsATruncatedTail(int trailing)
    {
        var file = Sl3File.Build([new Sl3Frame { Echo = Echo10 }], trailing);
        var reader = Reader(file);

        var frames = ReadAll(reader);

        Assert.Single(frames);
        Assert.Equal(new ChainBreak(186, trailing, ChainBreakReason.TruncatedTail), reader.Break);
    }

    [Fact]
    public void FortyEightBytesLeftAreReadAndNotCalledATruncatedTail()
    {
        var file = Sl3File.Build([new Sl3Frame { Echo = Echo10 }], trailing: 48);
        var reader = Reader(file);

        _ = ReadAll(reader);

        Assert.Equal(new ChainBreak(186, 48, ChainBreakReason.OffsetMismatch), reader.Break);
    }

    [Fact]
    public void AShortTailWithACorrectOffsetAndAPlausibleFrameSizeBreaksTheChainBySize()
    {
        var tail = Sl3File.Frame(new Sl3Frame { Echo = new byte[100] }, 8)[..100];
        using var file = new MemoryStream();
        file.Write(Sl3File.Build());
        file.Write(tail);
        var reader = new Sl3FrameReader(new MemoryStream(file.ToArray()));

        Assert.Null(reader.ReadNext());
        Assert.Equal(new ChainBreak(8, 100, ChainBreakReason.InvalidFrameSize), reader.Break);
    }

    [Fact]
    public void ABreakIsReportedOnceAndReadingStopsThere()
    {
        var file = Sl3File.Build([new Sl3Frame { Echo = Echo10 }], trailing: 20);
        var reader = Reader(file);
        _ = ReadAll(reader);

        Assert.Null(reader.ReadNext());
        Assert.Equal(ChainBreakReason.TruncatedTail, reader.Break!.Reason);
    }

    [Fact]
    public void HoldsPositionsAsSixtyFourBitIntegers()
    {
        using var stream = new VirtualSl3Stream(frameCount: 33_500);
        var reader = new Sl3FrameReader(stream);
        FrameRecord? last = null;
        var count = 0;

        while (reader.ReadNext() is { } frame)
        {
            last = frame;
            count++;
        }

        Assert.Equal(33_500, count);
        Assert.True(last!.Position > int.MaxValue);
        Assert.Equal(8 + (33_499L * VirtualSl3Stream.FrameBytes), last.Position);
        Assert.Equal(33_499u, last.FrameIndex);
        Assert.Equal(168, last.HeaderSize);
        Assert.Null(reader.Break);
    }

    [Fact]
    public void RefusesAMissingOrUnseekableStream()
    {
        Assert.Throws<ArgumentNullException>(() => new Sl3FrameReader(null!));
        using var stream = new NonSeekableStream();
        Assert.Throws<ArgumentException>(() => new Sl3FrameReader(stream));
    }
}
