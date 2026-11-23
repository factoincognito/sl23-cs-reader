namespace Sl23.Reader.Tests;

public class Sl2FrameReaderTests
{
    private static readonly byte[] Echo10 = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];

    private static List<FrameRecord> ReadAll(Sl2FrameReader reader)
    {
        var frames = new List<FrameRecord>();
        while (reader.ReadNext() is { } frame)
        {
            frames.Add(frame);
        }

        return frames;
    }

    private static Sl2FrameReader Reader(byte[] file, bool header = false, bool echo = false) =>
        new(new MemoryStream(file), header, echo);

    [Fact]
    public void AFileWithOnlyTheFileHeaderHasNoFramesAndNoBreak()
    {
        var reader = Reader(Sl2File.Build());

        Assert.Null(reader.ReadNext());
        Assert.Null(reader.Break);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(7)]
    public void AFileShorterThanTheFileHeaderHasNoFramesAndNoBreak(int length)
    {
        var reader = Reader(new byte[length]);

        Assert.Null(reader.ReadNext());
        Assert.Null(reader.Break);
    }

    [Fact]
    public void ReadsEveryFieldOfAFrame()
    {
        var file = Sl2File.Build(new Sl2Frame
        {
            Channel = 2,
            Echo = Echo10,
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
        });
        var reader = Reader(file);

        var frame = Assert.Single(ReadAll(reader));

        Assert.Equal(FileFormat.Sl2, frame.Format);
        Assert.Equal(8, frame.Position);
        Assert.Equal(8u, frame.OffsetField);
        Assert.Equal(144, frame.HeaderSize);
        Assert.Equal(154, frame.FrameSize);
        Assert.Equal(10, frame.EchoSize);
        Assert.Equal((ushort)2, frame.ChannelId);
        Assert.Equal("DownScan", frame.ChannelLabel);
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
        Assert.Null(frame.FrameVersion);
        Assert.Null(frame.PreviousFrameOffsets);
        Assert.Null(reader.Break);
    }

    [Fact]
    public void FramesFollowEachOtherByTheirFrameSize()
    {
        var file = Sl2File.Build(
            new Sl2Frame { Echo = Echo10, FrameIndex = 0 },
            new Sl2Frame { Channel = 1, Echo = new byte[300], FrameIndex = 1 },
            new Sl2Frame { FrameIndex = 2 });
        var reader = Reader(file);

        var frames = ReadAll(reader);

        Assert.Equal([8L, 162L, 606L], frames.Select(f => f.Position));
        Assert.Equal([0u, 1u, 2u], frames.Select(f => f.FrameIndex!.Value));
        Assert.Equal([(ushort)0, (ushort)1, (ushort)0], frames.Select(f => f.ChannelId));
        Assert.Null(reader.Break);
    }

    [Fact]
    public void ReadingPastTheEndKeepsReturningNull()
    {
        var reader = Reader(Sl2File.Build(new Sl2Frame { Echo = Echo10 }));
        _ = ReadAll(reader);

        Assert.Null(reader.ReadNext());
        Assert.Null(reader.ReadNext());
    }

    [Fact]
    public void SkipsTheEchoUnlessAskedForIt()
    {
        var file = Sl2File.Build(new Sl2Frame { Echo = Echo10 }, new Sl2Frame { FrameIndex = 9 });

        var frames = ReadAll(Reader(file));

        Assert.Null(frames[0].Echo);
        Assert.Null(frames[0].HeaderBytes);
        Assert.Equal(9u, frames[1].FrameIndex);
    }

    [Fact]
    public void CarriesTheEchoAndTheHeaderBytesWhenAskedFor()
    {
        var file = Sl2File.Build(new Sl2Frame { Echo = Echo10, FrameIndex = 5 }, new Sl2Frame { FrameIndex = 6 });

        var frames = ReadAll(Reader(file, header: true, echo: true));

        Assert.Equal(Echo10, frames[0].Echo!.Value.ToArray());
        Assert.Equal(144, frames[0].HeaderBytes!.Value.Length);
        Assert.Equal(5, frames[0].HeaderBytes!.Value.Span[36]);
        Assert.Equal(0, frames[1].Echo!.Value.Length);
        Assert.Equal(6u, frames[1].FrameIndex);
    }

    [Fact]
    public void ABrokenEchoSizeIsAFindingAndTheChainGoesOn()
    {
        var file = Sl2File.Build(
            new Sl2Frame { Echo = Echo10, EchoSize = 4, FrameIndex = 1 },
            new Sl2Frame { FrameIndex = 2 });
        var reader = Reader(file);

        var frames = ReadAll(reader);

        Assert.Equal(2, frames.Count);
        Assert.Equal(4, frames[0].EchoSize);
        Assert.Equal(154, frames[0].FrameSize);
        Assert.Equal(150, frames[0].HeaderSize);
        Assert.Equal(2u, frames[1].FrameIndex);
        Assert.Null(reader.Break);
    }

    [Fact]
    public void AWrongOffsetFieldBreaksTheChainAtThatFrame()
    {
        var file = Sl2File.Build(
            new Sl2Frame { Echo = Echo10, FrameIndex = 1 },
            new Sl2Frame { OffsetField = 999, Echo = Echo10, FrameIndex = 2 },
            new Sl2Frame { FrameIndex = 3 });
        var reader = Reader(file);

        var frames = ReadAll(reader);

        var frame = Assert.Single(frames);
        Assert.Equal(1u, frame.FrameIndex);
        Assert.Equal(new ChainBreak(162, file.Length - 162, ChainBreakReason.OffsetMismatch), reader.Break);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(143)]
    [InlineData(60000)]
    public void AFrameSizeBelowTheHeaderOrPastTheEndBreaksTheChain(int frameSize)
    {
        var file = Sl2File.Build(
            new Sl2Frame { Echo = Echo10 },
            new Sl2Frame { FrameSize = (ushort)frameSize, Echo = Echo10 });
        var reader = Reader(file);

        var frames = ReadAll(reader);

        Assert.Single(frames);
        Assert.Equal(new ChainBreak(162, file.Length - 162, ChainBreakReason.InvalidFrameSize), reader.Break);
    }

    [Fact]
    public void AFrameThatEndsOneByteBeyondTheFileBreaksTheChain()
    {
        var file = Sl2File.Build(new Sl2Frame { FrameSize = 145 });
        var reader = Reader(file);

        Assert.Null(reader.ReadNext());
        Assert.Equal(new ChainBreak(8, 144, ChainBreakReason.InvalidFrameSize), reader.Break);
    }

    [Fact]
    public void AFrameThatEndsExactlyAtTheEndOfTheFileIsRead()
    {
        var file = Sl2File.Build(new Sl2Frame { FrameSize = 144 + 3, Echo = [1, 2, 3], EchoSize = 3 });
        var reader = Reader(file);

        Assert.NotNull(reader.ReadNext());
        Assert.Null(reader.ReadNext());
        Assert.Null(reader.Break);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(20)]
    [InlineData(35)]
    public void FewerThanThirtySixBytesLeftIsATruncatedTail(int trailing)
    {
        var file = Sl2File.Build([new Sl2Frame { Echo = Echo10 }], trailing);
        var reader = Reader(file);

        var frames = ReadAll(reader);

        Assert.Single(frames);
        Assert.Equal(new ChainBreak(162, trailing, ChainBreakReason.TruncatedTail), reader.Break);
    }

    [Fact]
    public void ThirtySixBytesLeftAreReadAndNotCalledATruncatedTail()
    {
        var file = Sl2File.Build([new Sl2Frame { Echo = Echo10 }], trailing: 36);
        var reader = Reader(file);

        _ = ReadAll(reader);

        Assert.Equal(new ChainBreak(162, 36, ChainBreakReason.OffsetMismatch), reader.Break);
    }

    [Fact]
    public void ABreakIsReportedOnceAndReadingStopsThere()
    {
        var file = Sl2File.Build([new Sl2Frame { Echo = Echo10 }], trailing: 20);
        var reader = Reader(file);
        _ = ReadAll(reader);

        Assert.Null(reader.ReadNext());
        Assert.Equal(ChainBreakReason.TruncatedTail, reader.Break!.Reason);
    }

    [Fact]
    public void HoldsPositionsAsSixtyFourBitIntegers()
    {
        using var stream = new VirtualSl2Stream(frameCount: 33_500);
        var reader = new Sl2FrameReader(stream);
        FrameRecord? last = null;
        var count = 0;

        while (reader.ReadNext() is { } frame)
        {
            last = frame;
            count++;
        }

        Assert.Equal(33_500, count);
        Assert.True(last!.Position > int.MaxValue);
        Assert.Equal(8 + (33_499L * VirtualSl2Stream.FrameBytes), last.Position);
        Assert.Equal(33_499u, last.FrameIndex);
        Assert.Null(reader.Break);
    }

    [Fact]
    public void RefusesAMissingOrUnseekableStream()
    {
        Assert.Throws<ArgumentNullException>(() => new Sl2FrameReader(null!));
        using var stream = new NonSeekableStream();
        Assert.Throws<ArgumentException>(() => new Sl2FrameReader(stream));
    }
}
