using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Sl23.Reader.Cli;

namespace Sl23.Reader.Tests;

public sealed partial class InventoryCommandTests : IDisposable
{
    private readonly DirectoryInfo folder = Directory.CreateTempSubdirectory("sl23-command-");

    public void Dispose() => folder.Delete(recursive: true);

    private static (int Code, string Output, string Error) Run(long threshold, params string[] args)
    {
        using var output = new StringWriter(CultureInfo.InvariantCulture);
        using var error = new StringWriter(CultureInfo.InvariantCulture);
        var code = InventoryCommand.Run(args, output, error, threshold);
        return (code, output.ToString(), error.ToString());
    }

    private static (int Code, string Output, string Error) Run(params string[] args) => Run(InventoryCommand.DefaultProgressThreshold, args);

    private static string TextReport(params string[] paths)
    {
        using var writer = new StringWriter(CultureInfo.InvariantCulture);
        InventoryTextReport.Write(writer, [.. paths.Select(path => FileAnalyser.Analyse(path))], new ReportOptions());
        return writer.ToString();
    }

    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;

    private string Sl3Path(string name, int frames)
    {
        var path = Path.Combine(folder.FullName, name);
        File.WriteAllBytes(path, Sl3File.Build([.. Enumerable.Range(0, frames).Select(i => new Sl3Frame { FrameIndex = (uint)i, PreviousFrameSize = i == 0 ? (ushort)0 : (ushort)168 })]));
        return path;
    }

    [Fact]
    public void NoInputIsAUsageErrorAndNothingIsWritten()
    {
        var (code, output, error) = Run("--json");

        Assert.Equal(Program.UsageError, code);
        Assert.Equal(string.Empty, output);
        Assert.Contains("No input given.", error, StringComparison.Ordinal);
        Assert.Contains("Usage: sl23-cs-reader inventory", error, StringComparison.Ordinal);
    }

    [Fact]
    public void AnUnknownOptionStopsAtOnceBeforeAnyFileIsRead()
    {
        var path = Sl3Path("a.sl3", 1);

        var (code, output, error) = Run(path, "--frobnicate");

        Assert.Equal(Program.UsageError, code);
        Assert.Equal(string.Empty, output);
        Assert.Contains("Unknown option: --frobnicate", error, StringComparison.Ordinal);
        Assert.Contains("Usage: sl23-cs-reader inventory", error, StringComparison.Ordinal);
    }

    [Fact]
    public void AnUnknownOptionIsEchoedWithItsControlCharactersEscaped()
    {
        var (code, _, error) = Run("--bad\u001b[31m");

        Assert.Equal(Program.UsageError, code);
        Assert.Contains("Unknown option: --bad\\u001B[31m", error, StringComparison.Ordinal);
        Assert.DoesNotContain('\u001b', error);
    }

    private static (int Code, string Output, string Error) RunWithListing(Func<string, IEnumerable<string>> enumerateFiles, params string[] args)
    {
        using var output = new StringWriter(CultureInfo.InvariantCulture);
        using var error = new StringWriter(CultureInfo.InvariantCulture);
        var code = InventoryCommand.Run(args, output, error, InventoryCommand.DefaultProgressThreshold, enumerateFiles);
        return (code, output.ToString(), error.ToString());
    }

    [Theory]
    [InlineData(true, "access denied")]
    [InlineData(false, "read error")]
    public void AFolderThatCannotBeListedIsReportedAndFailsTheRunAndTheOthersAreStillRead(bool accessDenied, string reason)
    {
        var path = Sl3Path("a.sl3", 1);

        var (code, output, _) = RunWithListing(_ => throw (accessDenied ? new UnauthorizedAccessException() : new IOException()), path, folder.FullName);

        Assert.Equal(Program.InputFailed, code);
        Assert.Contains("File 2 of 2: could not be read: " + reason + "\n", output.ReplaceLineEndings("\n"), StringComparison.Ordinal);
        Assert.Contains("File 1 of 2: ", output, StringComparison.Ordinal);
        Assert.DoesNotContain("File 1 of 2: could not be read", output, StringComparison.Ordinal);
    }

    [Fact]
    public void AFolderThatCannotBeListedKeepsItsPlaceAmongTheInputs()
    {
        var path = Sl3Path("a.sl3", 1);

        var (_, first, _) = RunWithListing(_ => throw new UnauthorizedAccessException(), folder.FullName, path);
        var (_, last, _) = RunWithListing(_ => throw new UnauthorizedAccessException(), path, folder.FullName);

        Assert.Contains("File 1 of 2: could not be read: access denied", first.ReplaceLineEndings("\n"), StringComparison.Ordinal);
        Assert.Contains("File 2 of 2:", first, StringComparison.Ordinal);
        Assert.Contains("File 2 of 2: could not be read: access denied", last.ReplaceLineEndings("\n"), StringComparison.Ordinal);
    }

    [Fact]
    public void HelpWritesTheUsageToStandardOutputAndSucceeds()
    {
        var (code, output, error) = Run("--help");

        Assert.Equal(0, code);
        Assert.Contains("Usage: sl23-cs-reader inventory", output, StringComparison.Ordinal);
        Assert.Contains("--json", output, StringComparison.Ordinal);
        Assert.Contains("--details", output, StringComparison.Ordinal);
        Assert.Equal(string.Empty, error);
    }

    [Fact]
    public void OneFileGivesTheTextReportWithoutItsPath()
    {
        var path = Sl3Path("secret_2025-06-12.sl3", 2);

        var (code, output, error) = Run(path);

        Assert.Equal(0, code);
        Assert.Equal(TextReport(path), output);
        Assert.DoesNotContain("secret", output, StringComparison.Ordinal);
        Assert.DoesNotContain("2025", output, StringComparison.Ordinal);
        Assert.Equal(string.Empty, error);
    }

    [Fact]
    public void JsonGivesTheJsonReportInsteadOfTheText()
    {
        var path = Sl3Path("a.sl3", 2);

        var (code, output, _) = Run("--json", path);

        Assert.Equal(0, code);
        var root = Parse(output);
        Assert.Equal(1, root.GetProperty("fileCount").GetInt32());
        Assert.False(root.GetProperty("detailsIncluded").GetBoolean());
        Assert.Equal(2, root.GetProperty("files")[0].GetProperty("frames").GetInt32());
    }

    [Fact]
    public void DetailsAddTheFileNameToBothReports()
    {
        var path = Sl3Path("named.sl3", 1);

        Assert.Contains(path, Run("--details", path).Output, StringComparison.Ordinal);
        var json = Parse(Run(path, "--json", "--details").Output);
        Assert.True(json.GetProperty("detailsIncluded").GetBoolean());
        Assert.Equal(path, json.GetProperty("files")[0].GetProperty("name").GetString());
    }

    [Fact]
    public void SeveralFilesAreReportedInTheOrderGiven()
    {
        var one = Sl3Path("one.sl3", 1);
        var three = Sl3Path("three.sl3", 3);

        var files = Parse(Run("--json", three, one).Output).GetProperty("files");

        Assert.Equal(3, files[0].GetProperty("frames").GetInt32());
        Assert.Equal(1, files[1].GetProperty("frames").GetInt32());
    }

    [Fact]
    public void AFolderMeansTheSl2AndSl3FilesDirectlyInItSortedByName()
    {
        var input = Directory.CreateDirectory(Path.Combine(folder.FullName, "input")).FullName;
        File.WriteAllBytes(Path.Combine(input, "b.SL3"), Sl3File.Build(new Sl3Frame(), new Sl3Frame()));
        File.WriteAllBytes(Path.Combine(input, "a.sl2"), Sl2File.Build(new Sl2Frame()));
        File.WriteAllText(Path.Combine(input, "c.txt"), "not a sonar file");
        File.WriteAllBytes(Path.Combine(input, "d.sl2.bak"), Sl2File.Build(new Sl2Frame()));
        var below = Directory.CreateDirectory(Path.Combine(input, "below")).FullName;
        File.WriteAllBytes(Path.Combine(below, "e.sl2"), Sl2File.Build(new Sl2Frame()));

        var (code, output, _) = Run("--json", "--details", input);

        Assert.Equal(0, code);
        var files = Parse(output).GetProperty("files");
        Assert.Equal(2, files.GetArrayLength());
        Assert.Equal(Path.Combine(input, "a.sl2"), files[0].GetProperty("name").GetString());
        Assert.Equal(Path.Combine(input, "b.SL3"), files[1].GetProperty("name").GetString());
    }

    [Fact]
    public void AFolderWithNoSonarFilesIsReportedAndFailsTheRun()
    {
        var input = Directory.CreateDirectory(Path.Combine(folder.FullName, "empty")).FullName;

        var (code, output, error) = Run("--json", input);

        Assert.Equal(Program.InputFailed, code);
        Assert.Equal(0, Parse(output).GetProperty("fileCount").GetInt32());
        Assert.Contains("A folder holds no .sl2 or .sl3 files.", error, StringComparison.Ordinal);
        Assert.DoesNotContain(input, error, StringComparison.Ordinal);
    }

    [Fact]
    public void AMissingPathIsReportedTheOthersAreStillReadAndTheExitCodeIsNonZero()
    {
        var good = Sl3Path("good.sl3", 2);
        var missing = Path.Combine(folder.FullName, "missing.sl3");

        var (code, output, _) = Run("--json", missing, good);

        Assert.Equal(Program.InputFailed, code);
        var files = Parse(output).GetProperty("files");
        Assert.Equal("notFound", files[0].GetProperty("problem").GetString());
        Assert.Equal(JsonValueKind.Null, files[1].GetProperty("problem").ValueKind);
        Assert.Equal(2, files[1].GetProperty("frames").GetInt32());
    }

    [Fact]
    public void ThePathOfAMissingFileIsNotInTheDefaultTextReportOrOnStandardError()
    {
        var missing = Path.Combine(folder.FullName, "secret-missing.sl3");

        var (code, output, error) = Run(missing);

        Assert.Equal(Program.InputFailed, code);
        Assert.DoesNotContain("secret", output, StringComparison.Ordinal);
        Assert.DoesNotContain("secret", error, StringComparison.Ordinal);
        Assert.Contains("not found", output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void APathThroughAFileIsNotFoundAndFailsTheRun()
    {
        var file = Sl3Path("a.sl3", 1);

        var (code, output, _) = Run("--json", Path.Combine(file, "x"));

        Assert.Equal(Program.InputFailed, code);
        Assert.Equal("notFound", Parse(output).GetProperty("files")[0].GetProperty("problem").GetString());
    }

    [Fact]
    public void FindingsInsideAFileDoNotChangeTheExitCode()
    {
        var path = Path.Combine(folder.FullName, "damaged.sl3");
        File.WriteAllBytes(path, Sl3File.Build([new Sl3Frame { FrameIndex = 5 }, new Sl3Frame { FrameIndex = 1 }], trailing: 9));

        var (code, output, _) = Run("--json", path);

        Assert.Equal(0, code);
        var file = Parse(output).GetProperty("files")[0];
        Assert.Equal("truncatedTail", file.GetProperty("chain").GetProperty("break").GetProperty("reason").GetString());
    }

    [Fact]
    public void AFileOfAnotherFormatOrTooShortIsReportedAsNotReadWithoutFailingTheRun()
    {
        var slg = Path.Combine(folder.FullName, "a.slg");
        File.WriteAllBytes(slg, [1, 0, 0, 0, 0, 0, 0, 0, 9]);
        var shortFile = Path.Combine(folder.FullName, "b.sl2");
        File.WriteAllBytes(shortFile, [2, 0]);

        var (code, output, _) = Run("--json", slg, shortFile);

        Assert.Equal(0, code);
        var files = Parse(output).GetProperty("files");
        Assert.Equal("formatNotRead", files[0].GetProperty("problem").GetString());
        Assert.Equal("tooShortForHeader", files[1].GetProperty("problem").GetString());
    }

    [Fact]
    public void AnEmptyPathIsReportedAsNotFoundAndTheOthersAreStillRead()
    {
        var good = Sl3Path("good.sl3", 1);

        var (code, output, error) = Run(1, "--json", string.Empty, good);

        Assert.Equal(Program.InputFailed, code);
        var files = Parse(output).GetProperty("files");
        Assert.Equal("notFound", files[0].GetProperty("problem").GetString());
        Assert.Equal(1, files[1].GetProperty("frames").GetInt32());
        Assert.DoesNotContain("Exception", error, StringComparison.Ordinal);
    }

    [Fact]
    public void AfterTwoDashesEverythingIsAPath()
    {
        var (code, output, _) = Run("--json", "--", "--json");

        Assert.Equal(Program.InputFailed, code);
        Assert.Equal("notFound", Parse(output).GetProperty("files")[0].GetProperty("problem").GetString());
    }

    [Fact]
    public void NoProgressIsWrittenForSmallFiles()
    {
        var path = Sl3Path("a.sl3", 3);

        var (_, _, error) = Run(path);

        Assert.Equal(string.Empty, error);
    }

    [Fact]
    public void ALargeFileWritesProgressToStandardErrorOnlyAndWithoutItsName()
    {
        var path = Path.Combine(folder.FullName, "big_secret.sl2");
        File.WriteAllBytes(path, Sl2File.Build([.. Enumerable.Range(0, 24).Select(i => new Sl2Frame { FrameIndex = (uint)i, PreviousFrameSize = (ushort)(i == 0 ? 0 : 65535), Echo = new byte[65535 - 144] })]));
        var small = Sl3Path("small.sl3", 1);

        var (code, output, error) = Run(threshold: 1_000_000, "--json", path, small);

        Assert.Equal(0, code);
        Assert.Equal(24, Parse(output).GetProperty("files")[0].GetProperty("frames").GetInt32());
        var lines = error.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        Assert.NotEmpty(lines);
        Assert.All(lines, line => Assert.Matches(ProgressLine(), line));
        Assert.All(lines, line => Assert.StartsWith("File 1 of 2: ", line, StringComparison.Ordinal));
        var percents = lines.Select(line => int.Parse(line.Split(' ')[4].TrimEnd('%'), CultureInfo.InvariantCulture)).ToList();
        Assert.Equal(percents.Order().ToList(), percents);
        Assert.Equal(percents.Distinct().Count(), percents.Count);
        Assert.All(percents, percent => Assert.InRange(percent, 1, 100));
        Assert.DoesNotContain("secret", error, StringComparison.Ordinal);
    }

    [Fact]
    public void ProgressNumbersCountAFailedFolderAndLeaveOutAFolderWithNoFiles()
    {
        var path = Path.Combine(folder.FullName, "big.sl2");
        File.WriteAllBytes(path, Sl2File.Build([.. Enumerable.Range(0, 24).Select(i => new Sl2Frame { FrameIndex = (uint)i, PreviousFrameSize = (ushort)(i == 0 ? 0 : 65535), Echo = new byte[65535 - 144] })]));
        var empty = Directory.CreateDirectory(Path.Combine(folder.FullName, "empty")).FullName;
        using var output = new StringWriter(CultureInfo.InvariantCulture);
        using var error = new StringWriter(CultureInfo.InvariantCulture);

        var code = InventoryCommand.Run([folder.FullName, empty, path], output, error, 1_000_000, dir => dir == empty ? [] : throw new UnauthorizedAccessException());

        Assert.Equal(Program.InputFailed, code);
        Assert.Contains("A folder holds no .sl2 or .sl3 files.", error.ToString(), StringComparison.Ordinal);
        var lines = error.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).Where(line => line.StartsWith("File ", StringComparison.Ordinal)).ToList();
        Assert.NotEmpty(lines);
        Assert.All(lines, line => Assert.StartsWith("File 2 of 2: ", line, StringComparison.Ordinal));
        Assert.Contains("File 1 of 2: could not be read: access denied", output.ToString().ReplaceLineEndings("\n"), StringComparison.Ordinal);
    }

    [Fact]
    public void ProgressStartsAtTheThresholdSizeExactly()
    {
        var path = Path.Combine(folder.FullName, "edge.sl2");
        File.WriteAllBytes(path, Sl2File.Build([.. Enumerable.Range(0, 24).Select(i => new Sl2Frame { FrameIndex = (uint)i, PreviousFrameSize = (ushort)(i == 0 ? 0 : 65535), Echo = new byte[65535 - 144] })]));
        var size = new FileInfo(path).Length;

        Assert.NotEqual(string.Empty, Run(size, path).Error);
        Assert.Equal(string.Empty, Run(size + 1, path).Error);
    }

    [Fact]
    public void ARunWithoutArgumentsOrWritersIsRefused()
    {
        using var writer = new StringWriter(CultureInfo.InvariantCulture);

        Assert.Throws<ArgumentNullException>(() => InventoryCommand.Run(null!, writer, writer));
        Assert.Throws<ArgumentNullException>(() => InventoryCommand.Run([], null!, writer));
        Assert.Throws<ArgumentNullException>(() => InventoryCommand.Run([], writer, null!));
    }

    [Fact]
    public void TheProgramRunsTheInventoryCommand()
    {
        var path = Sl3Path("a.sl3", 1);
        using var output = new StringWriter(CultureInfo.InvariantCulture);
        using var error = new StringWriter(CultureInfo.InvariantCulture);

        var code = Program.Run(["inventory", path], output, error);

        Assert.Equal(0, code);
        Assert.Equal(TextReport(path), output.ToString());
    }

    [GeneratedRegex(@"^File \d+ of \d+: \d+% read$")]
    private static partial Regex ProgressLine();
}
