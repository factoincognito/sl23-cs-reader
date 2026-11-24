namespace Sl23.Reader.Cli;

/// <summary>The command-line shell's entry point.</summary>
public static class Program
{
    /// <summary>The exit code for a usage error: no command, an unknown command or option, or no input.</summary>
    public const int UsageError = 2;

    /// <summary>The exit code when an input could not be read; the other inputs were still processed.</summary>
    public const int InputFailed = 1;

    /// <summary>Runs the shell with the process's standard streams.</summary>
    /// <param name="args">The command-line arguments.</param>
    /// <returns>The exit code.</returns>
    public static int Main(string[] args) => Run(args, Console.Out, Console.Error);

    /// <summary>Runs the shell, writing to the given writers, so tests can read what it writes.</summary>
    /// <param name="args">The command-line arguments.</param>
    /// <param name="output">Where results go (standard output).</param>
    /// <param name="error">Where messages and usage go (standard error).</param>
    /// <returns>The exit code: <see cref="UsageError"/> for a usage error.</returns>
    public static int Run(string[] args, TextWriter output, TextWriter error)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);

        if (args.Length > 0 && args[0] == "inventory")
        {
            return InventoryCommand.Run(args[1..], output, error);
        }

        if (args.Length == 0)
        {
            error.WriteLine("No command given.");
        }
        else
        {
            error.WriteLine($"Unknown command: {Printable(args[0])}");
        }

        error.WriteLine("Usage: sl23-cs-reader <command> [options]");
        error.WriteLine("Commands: inventory");
        return UsageError;
    }

    /// <summary>Text for a message: each control character is written as \uXXXX, so an argument cannot move the cursor or recolour a terminal.</summary>
    /// <param name="text">The text the user gave.</param>
    /// <returns>The text with control characters escaped.</returns>
    internal static string Printable(string text) =>
        string.Concat(text.Select(c => char.IsControl(c) ? string.Create(System.Globalization.CultureInfo.InvariantCulture, $"\\u{(int)c:X4}") : c.ToString()));
}
