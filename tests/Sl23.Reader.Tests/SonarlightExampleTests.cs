using System.Security.Cryptography;

namespace Sl23.Reader.Tests;

/// <summary>
/// The analyser on real files: the two public example recordings of sonarlight (design 7.2 and 9).
/// The first 40 frames of each are in <c>TestData/sonarlight/</c> and are read in every run. The
/// whole files are not in the repository: their tests run when the environment variable
/// <see cref="ExamplesFolderVariable"/> names a folder that holds them, and are skipped otherwise.
/// </summary>
public class SonarlightExampleTests
{
    /// <summary>The environment variable that names the folder with the two whole example files.</summary>
    public const string ExamplesFolderVariable = "SL23_SONARLIGHT_EXAMPLES";

    private const string Sl2Example = "example_sl2_file.sl2";
    private const string Sl3Example = "example_sl3_file.sl3";
    private const string Sl2ExampleSha256 = "126553ff01a460384d8dab1a4d5ef13e27f9040618ff9bd841ce9f1149385d08";
    private const string Sl3ExampleSha256 = "8208d90733bd0487f1a9417398cb85d433f184a4539f703778ba5d8fd2efe285";

    private const string Sl2Excerpt = "example_sl2_file.first-40-frames.sl2";
    private const string Sl3Excerpt = "example_sl3_file.first-40-frames.sl3";
    private const string Sl2ExcerptSha256 = "44ea9efdfc996e5232b0568b2cf120fb22b718914ccfa62efc6d0efc6292ab53";
    private const string Sl3ExcerptSha256 = "59786cefee9b8ab52743ebcc3c12989d4ec129774ef3305a8406a5fa38d05686";

    private static string ExcerptPath(string name) => Path.Combine(AppContext.BaseDirectory, "TestData", "sonarlight", name);

    private static string Sha256Of(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexStringLower(SHA256.HashData(stream));
    }

    private static FileInventory Read(string path)
    {
        var report = FileAnalyser.Analyse(path);
        Assert.Equal(FileProblem.None, report.Problem);
        Assert.NotNull(report.Inventory);
        return report.Inventory;
    }

    private static FileInventory ReadWholeExample(string name, string sha256)
    {
        var folder = Environment.GetEnvironmentVariable(ExamplesFolderVariable);
        if (string.IsNullOrEmpty(folder))
        {
            Assert.Skip($"The whole sonarlight example files are not in the repository. Set {ExamplesFolderVariable} to a folder that holds {Sl2Example} and {Sl3Example} to run this test.");
        }

        var path = Path.Combine(folder, name);
        Assert.True(File.Exists(path), $"{ExamplesFolderVariable} is set, but its folder does not hold {name}.");
        Assert.Equal(sha256, Sha256Of(path));
        return Read(path);
    }

    private static Dictionary<ushort, long> FramesPerChannel(FileInventory inventory) =>
        inventory.Channels.ToDictionary(channel => channel.ChannelId, channel => channel.FrameCount);

    private static void AssertFramingHolds(FileInventory inventory)
    {
        Assert.Null(inventory.Break);
        Assert.Equal(inventory.FrameCount, inventory.Checks[1].Passed);
        Assert.Equal(inventory.FrameCount, inventory.Checks[2].Passed);
        Assert.Equal(inventory.FrameCount, inventory.Checks[3].Passed);
        Assert.Equal(1, inventory.Checks[4].Passed);
        for (var check = 1; check <= 4; check++)
        {
            Assert.Equal(0, inventory.Checks[check].Failed);
        }
    }

    [Theory]
    [InlineData(Sl2Excerpt, 82_608, Sl2ExcerptSha256)]
    [InlineData(Sl3Excerpt, 82_768, Sl3ExcerptSha256)]
    public void AnExcerptInTheRepositoryIsByteForByteTheStartOfItsExampleFile(string name, long size, string sha256)
    {
        var path = ExcerptPath(name);

        Assert.Equal(size, new FileInfo(path).Length);
        Assert.Equal(sha256, Sha256Of(path));
    }

    [Fact]
    public void TheSl2ExcerptHoldsFortyFramesInTheOneSl2Layout()
    {
        var inventory = Read(ExcerptPath(Sl2Excerpt));

        Assert.Equal(FileFormat.Sl2, inventory.Header.Format);
        Assert.Equal(82_608, inventory.FileSize);
        Assert.Equal(40, inventory.FrameCount);
        Assert.Equal(new Dictionary<long, long> { [144] = 40 }, inventory.Layouts);
        Assert.Equal(new Dictionary<ushort, long> { [0] = 5, [1] = 5, [2] = 5, [5] = 5, [10] = 10, [11] = 10 }, FramesPerChannel(inventory));
        AssertFramingHolds(inventory);
    }

    [Fact]
    public void TheSl3ExcerptHoldsTwentyFramesOfEachOfItsTwoHeaderSizes()
    {
        var inventory = Read(ExcerptPath(Sl3Excerpt));

        Assert.Equal(FileFormat.Sl3, inventory.Header.Format);
        Assert.Equal(82_768, inventory.FileSize);
        Assert.Equal(40, inventory.FrameCount);
        Assert.Equal(new Dictionary<long, long> { [128] = 20, [168] = 20 }, inventory.Layouts);
        Assert.Equal(new Dictionary<ushort, long> { [0] = 5, [1] = 5, [2] = 5, [5] = 5, [7] = 10, [8] = 10 }, FramesPerChannel(inventory));
        AssertFramingHolds(inventory);
    }

    [Fact]
    public void TheSl3ExcerptHasItsShortHeadersOnChannels7And8Only()
    {
        var inventory = Read(ExcerptPath(Sl3Excerpt));

        Assert.All(inventory.Channels, channel =>
        {
            var expected = channel.ChannelId is 7 or 8 ? 128 : 168;
            Assert.Equal(new Dictionary<long, long> { [expected] = channel.FrameCount }, channel.HeaderSizes);
        });
    }

    [Fact]
    public void TheWholeSl2ExampleHolds10304Frames()
    {
        var inventory = ReadWholeExample(Sl2Example, Sl2ExampleSha256);

        Assert.Equal(FileFormat.Sl2, inventory.Header.Format);
        Assert.Equal(10_304, inventory.FrameCount);
        Assert.Equal(new Dictionary<long, long> { [144] = 10_304 }, inventory.Layouts);
        Assert.Equal(new Dictionary<ushort, long> { [0] = 1_288, [1] = 1_288, [2] = 1_288, [5] = 1_288, [10] = 2_576, [11] = 2_576 }, FramesPerChannel(inventory));
        AssertFramingHolds(inventory);
    }

    [Fact]
    public void TheWholeSl3ExampleHolds10250FramesOfWhich5124HaveA128ByteHeaderAnd5126A168ByteOne()
    {
        var inventory = ReadWholeExample(Sl3Example, Sl3ExampleSha256);

        Assert.Equal(FileFormat.Sl3, inventory.Header.Format);
        Assert.Equal(10_250, inventory.FrameCount);
        Assert.Equal(new Dictionary<long, long> { [128] = 5_124, [168] = 5_126 }, inventory.Layouts);
        Assert.Equal(new Dictionary<ushort, long> { [0] = 1_281, [1] = 1_281, [2] = 1_282, [5] = 1_282, [7] = 2_562, [8] = 2_562 }, FramesPerChannel(inventory));
        AssertFramingHolds(inventory);
    }
}
