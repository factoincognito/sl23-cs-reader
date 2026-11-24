using System.Globalization;

namespace Sl23.Reader.Cli;

/// <summary>The <c>inventory</c> command (design section 4): one report on the files and folders named.</summary>
public static class InventoryCommand
{
    /// <summary>The file size from which progress goes to standard error.</summary>
    public const long DefaultProgressThreshold = 64L << 20;

    private const string Usage = """
        Usage: sl23-cs-reader inventory [--json] [--details] <path>...
          <path>      an .sl2 or .sl3 file, or a folder (the .sl2 and .sl3 files directly in it)
          --json      write the report as JSON instead of text
          --details   add file names, depths and the range-limit pairs (not for sharing)
          --help      show this text
        """;

    /// <summary>Runs the command.</summary>
    /// <param name="args">The arguments after the command name.</param>
    /// <param name="output">Standard output: the report.</param>
    /// <param name="error">Standard error: messages and progress.</param>
    /// <param name="progressThreshold">The file size in bytes from which progress is written.</param>
    /// <param name="enumerateFiles">Lists the files directly in a folder; the default is <see cref="Directory.EnumerateFiles(string)"/>, and tests give one that fails.</param>
    /// <returns>The exit code: 0, <see cref="Program.InputFailed"/> when an input could not be read, or <see cref="Program.UsageError"/>.</returns>
    public static int Run(IReadOnlyList<string> args, TextWriter output, TextWriter error, long progressThreshold = DefaultProgressThreshold, Func<string, IEnumerable<string>>? enumerateFiles = null)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);

        var json = false;
        var details = false;
        var help = false;
        var optionsEnded = false;
        var inputs = new List<string>();
        foreach (var arg in args)
        {
            if (optionsEnded || !arg.StartsWith('-'))
            {
                inputs.Add(arg);
                continue;
            }

            switch (arg)
            {
                case "--":
                    optionsEnded = true;
                    break;
                case "--json":
                    json = true;
                    break;
                case "--details":
                    details = true;
                    break;
                case "--help":
                    help = true;
                    break;
                default:
                    error.WriteLine($"Unknown option: {Program.Printable(arg)}");
                    error.WriteLine(Usage);
                    return Program.UsageError;
            }
        }

        if (help)
        {
            output.WriteLine(Usage);
            return 0;
        }

        if (inputs.Count == 0)
        {
            error.WriteLine("No input given.");
            error.WriteLine(Usage);
            return Program.UsageError;
        }

        enumerateFiles ??= Directory.EnumerateFiles;
        var failed = false;
        var items = new List<(FileReport? Failure, string? Path)>();
        foreach (var input in inputs)
        {
            if (!Directory.Exists(input))
            {
                items.Add((null, input));
                continue;
            }

            var listed = List(input, enumerateFiles);
            if (listed.Problem != FileProblem.None)
            {
                items.Add((new FileReport(input, null, listed.Problem), null));
                failed = true;
            }
            else if (listed.Files.Count == 0)
            {
                error.WriteLine("A folder holds no .sl2 or .sl3 files.");
                failed = true;
            }

            items.AddRange(listed.Files.Select(file => ((FileReport?)null, (string?)file)));
        }

        var results = new List<FileReport>();
        var number = 0;
        foreach (var (failure, path) in items)
        {
            number++;
            results.Add(failure ?? FileAnalyser.Analyse(path!, ProgressTo(error, path!, number, items.Count, progressThreshold)));
        }

        failed |= results.Any(result => result.Problem is FileProblem.NotFound or FileProblem.AccessDenied or FileProblem.ReadFailed);
        var options = new ReportOptions(details);
        if (json)
        {
            InventoryJsonReport.Write(output, results, options);
        }
        else
        {
            InventoryTextReport.Write(output, results, options);
        }

        return failed ? Program.InputFailed : 0;
    }

    private static (FileProblem Problem, List<string> Files) List(string folder, Func<string, IEnumerable<string>> enumerateFiles)
    {
        try
        {
            var files = enumerateFiles(folder)
                .Where(file => file.EndsWith(".sl2", StringComparison.OrdinalIgnoreCase) || file.EndsWith(".sl3", StringComparison.OrdinalIgnoreCase))
                .Order(StringComparer.Ordinal)
                .ToList();
            return (FileProblem.None, files);
        }
        catch (UnauthorizedAccessException)
        {
            return (FileProblem.AccessDenied, []);
        }
        catch (IOException)
        {
            return (FileProblem.ReadFailed, []);
        }
    }

    private static Action<long>? ProgressTo(TextWriter error, string path, int number, int count, long threshold)
    {
        long size;
        try
        {
            size = new FileInfo(path).Length;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            // NotSupportedException comes from Windows path rules (a colon that is not part of a drive name), so no test on
            // another platform reaches it. Without a size there is no progress.
            return null;
        }

        if (size < threshold || size == 0)
        {
            return null;
        }

        var lastStep = -1L;
        return position =>
        {
            var percent = Math.Min(100L, position * 100 / size);
            if (percent / 10 == lastStep)
            {
                return;
            }

            lastStep = percent / 10;
            error.WriteLine(string.Create(CultureInfo.InvariantCulture, $"File {number} of {count}: {percent}% read"));
        };
    }
}
