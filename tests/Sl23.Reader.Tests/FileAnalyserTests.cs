namespace Sl23.Reader.Tests;

public sealed class FileAnalyserTests : IDisposable
{
    private readonly DirectoryInfo folder = Directory.CreateTempSubdirectory("sl23-analyser-");

    public void Dispose() => folder.Delete(recursive: true);

    [Fact]
    public void AnSl3StreamIsReadThroughToItsInventory()
    {
        var bytes = Sl3File.Build(new Sl3Frame { Channel = 0 }, new Sl3Frame { Channel = 1, PreviousFrameSize = 168 });
        using var stream = new MemoryStream(bytes);

        var report = FileAnalyser.Analyse(stream, "a.sl3");

        Assert.Equal("a.sl3", report.Path);
        Assert.Equal(FileProblem.None, report.Problem);
        Assert.Equal(bytes.Length, report.Size);
        Assert.Equal(FileFormat.Sl3, report.Header!.Value.Format);
        Assert.NotNull(report.Inventory);
        Assert.Equal(2, report.Inventory.FrameCount);
        Assert.Null(report.Inventory.Break);
        Assert.Equal(bytes.Length, report.Inventory.FileSize);
    }

    [Fact]
    public void AnSl2StreamIsReadThroughToItsInventory()
    {
        var bytes = Sl2File.Build(new Sl2Frame { Channel = 3 }, new Sl2Frame { Channel = 3, FrameIndex = 1, PreviousFrameSize = 144 });
        using var stream = new MemoryStream(bytes);

        var report = FileAnalyser.Analyse(stream, "a.sl2");

        Assert.Equal(FileFormat.Sl2, report.Inventory!.Header.Format);
        Assert.Equal(2, report.Inventory.FrameCount);
        Assert.Single(report.Inventory.Layouts);
    }

    [Fact]
    public void ADamagedTailIsAFindingInTheInventoryNotAProblem()
    {
        using var stream = new MemoryStream(Sl3File.Build([new Sl3Frame()], trailing: 10));

        var report = FileAnalyser.Analyse(stream, "damaged.sl3");

        Assert.Equal(FileProblem.None, report.Problem);
        Assert.Equal(ChainBreakReason.TruncatedTail, report.Inventory!.Break!.Reason);
        Assert.Equal(10, report.Inventory.Break.BytesLeft);
    }

    [Fact]
    public void AFileWithNoFramesHasAnInventoryOfZeroFrames()
    {
        using var stream = new MemoryStream([2, 0, 0, 0, 0, 0, 0, 0]);

        var report = FileAnalyser.Analyse(stream, "empty.sl2");

        Assert.Equal(0, report.Inventory!.FrameCount);
        Assert.Null(report.Inventory.Break);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    public void AFileShorterThanTheHeaderIsReportedWithItsSizeAndNoInventory(int size)
    {
        using var stream = new MemoryStream(new byte[size]);

        var report = FileAnalyser.Analyse(stream, "short.sl2");

        Assert.Equal(FileProblem.TooShortForHeader, report.Problem);
        Assert.Equal(size, report.Size);
        Assert.Null(report.Inventory);
        Assert.Null(report.Header);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(9)]
    public void AnSlgOrUnknownFileIsNotReadFurtherButKeepsItsHeader(int code)
    {
        using var stream = new MemoryStream([(byte)code, 0, 5, 0, 6, 0, 7, 0, 1, 2, 3]);

        var report = FileAnalyser.Analyse(stream, "other.slg");

        Assert.Equal(FileProblem.FormatNotRead, report.Problem);
        Assert.Equal(11, report.Size);
        Assert.Equal(new FileHeader((ushort)code, 5, 6, 7), report.Header);
        Assert.Null(report.Inventory);
    }

    [Fact]
    public void AFileReadFromItsPathGivesTheSameResultAsItsStream()
    {
        var bytes = Sl3File.Build(new Sl3Frame(), new Sl3Frame { Channel = 1 });
        var path = Path.Combine(folder.FullName, "a.sl3");
        File.WriteAllBytes(path, bytes);
        using var stream = new MemoryStream(bytes);

        var fromPath = FileAnalyser.Analyse(path);
        var fromStream = FileAnalyser.Analyse(stream, path);

        Assert.Equal(path, fromPath.Path);
        Assert.Equal(fromStream.Size, fromPath.Size);
        Assert.Equal(fromStream.Inventory!.FrameCount, fromPath.Inventory!.FrameCount);
        Assert.Equal(fromStream.Header, fromPath.Header);
    }

    [Fact]
    public void AReadOnlyFileCanBeRead()
    {
        var path = Path.Combine(folder.FullName, "readonly.sl3");
        File.WriteAllBytes(path, Sl3File.Build(new Sl3Frame()));
        File.SetAttributes(path, FileAttributes.ReadOnly);
        try
        {
            var report = FileAnalyser.Analyse(path);

            Assert.Equal(FileProblem.None, report.Problem);
            Assert.Equal(1, report.Inventory!.FrameCount);
        }
        finally
        {
            File.SetAttributes(path, FileAttributes.Normal);
        }
    }

    [Fact]
    public void AMissingFileIsNotFound()
    {
        var report = FileAnalyser.Analyse(Path.Combine(folder.FullName, "missing.sl2"));

        Assert.Equal(FileProblem.NotFound, report.Problem);
        Assert.Null(report.Inventory);
        Assert.Null(report.Size);
    }

    [Fact]
    public void AMissingFolderInThePathIsNotFound()
    {
        var report = FileAnalyser.Analyse(Path.Combine(folder.FullName, "nope", "missing.sl2"));

        Assert.Equal(FileProblem.NotFound, report.Problem);
    }

    [Fact]
    public void AFolderGivenAsAFileIsAccessDenied()
    {
        var report = FileAnalyser.Analyse(folder.FullName);

        Assert.Equal(FileProblem.AccessDenied, report.Problem);
    }

    [Fact]
    public void AStreamThatFailsMidwayIsReadFailedWithItsSize()
    {
        using var stream = new FailingStream(Sl3File.Build(new Sl3Frame(), new Sl3Frame()), failAtPosition: 100, new IOException("secret message with /home/adam/x.sl3"));

        var report = FileAnalyser.Analyse(stream, "x.sl3");

        Assert.Equal(FileProblem.ReadFailed, report.Problem);
        Assert.Equal(stream.Length, report.Size);
        Assert.Null(report.Inventory);
    }

    [Fact]
    public void AFailureOfAnotherKindInsideTheReadingIsReadFailedNotNotFound()
    {
        using var stream = new FailingStream(Sl3File.Build(new Sl3Frame(), new Sl3Frame()), failAtPosition: 100, new NotSupportedException());

        var report = FileAnalyser.Analyse(stream, "x.sl3");

        Assert.Equal(FileProblem.ReadFailed, report.Problem);
    }

    [Fact]
    public void AFileThatAnotherReaderHasOpenCanStillBeRead()
    {
        var path = Path.Combine(folder.FullName, "shared.sl3");
        File.WriteAllBytes(path, Sl3File.Build(new Sl3Frame()));
        using var other = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);

        var report = FileAnalyser.Analyse(path);

        Assert.Equal(FileProblem.None, report.Problem);
        Assert.Equal(1, report.Inventory!.FrameCount);
    }

    [Fact]
    public void AStreamThatIsRefusedMidwayIsAccessDenied()
    {
        using var stream = new FailingStream(Sl3File.Build(new Sl3Frame()), failAtPosition: 0, new UnauthorizedAccessException());

        var report = FileAnalyser.Analyse(stream, "x.sl3");

        Assert.Equal(FileProblem.AccessDenied, report.Problem);
    }

    [Fact]
    public void ProgressIsCalledAfterEveryStepOfBytesWithRisingPositions()
    {
        using var stream = new VirtualSl2Stream(frameCount: 40);
        var positions = new List<long>();

        var report = FileAnalyser.Analyse(stream, "big.sl2", positions.Add);

        Assert.Equal(40, report.Inventory!.FrameCount);
        Assert.True(positions.Count >= 2, "at least two calls for 2.6 MB");
        Assert.Equal(positions.Order().ToList(), positions);
        Assert.Equal(positions.Distinct().Count(), positions.Count);
        Assert.All(positions, position => Assert.InRange(position, FileAnalyser.ProgressStepBytes, stream.Length));
        for (var i = 1; i < positions.Count; i++)
        {
            Assert.True(positions[i] - positions[i - 1] >= FileAnalyser.ProgressStepBytes);
        }
    }

    [Fact]
    public void AFileSmallerThanOneStepGetsNoProgressCall()
    {
        using var stream = new MemoryStream(Sl3File.Build(new Sl3Frame(), new Sl3Frame()));
        var calls = 0;

        FileAnalyser.Analyse(stream, "small.sl3", _ => calls++);

        Assert.Equal(0, calls);
    }

    [Fact]
    public void ARunWithoutAStreamOrPathIsRefused()
    {
        Assert.Throws<ArgumentNullException>(() => FileAnalyser.Analyse((Stream)null!, "x"));
        Assert.Throws<ArgumentNullException>(() => FileAnalyser.Analyse(new MemoryStream(), null!));
        Assert.Throws<ArgumentNullException>(() => FileAnalyser.Analyse((string)null!));
    }

    private sealed class FailingStream(byte[] bytes, long failAtPosition, Exception failure) : Stream
    {
        private readonly MemoryStream inner = new(bytes);

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => false;

        public override long Length => inner.Length;

        public override long Position
        {
            get => inner.Position;
            set => inner.Position = value;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (inner.Position >= failAtPosition)
            {
                throw failure;
            }

            return inner.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin) => inner.Seek(offset, origin);

        public override void Flush()
        {
        }

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                inner.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
