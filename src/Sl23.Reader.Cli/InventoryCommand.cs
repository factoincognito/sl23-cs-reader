namespace Sl23.Reader.Cli;

/// <summary>The <c>inventory</c> command (design section 4).</summary>
public static class InventoryCommand
{
    /// <summary>The file size from which progress goes to standard error.</summary>
    public const long DefaultProgressThreshold = 64L << 20;

    /// <summary>Runs the command.</summary>
    /// <param name="args">The arguments after the command name.</param>
    /// <param name="output">Standard output: the report.</param>
    /// <param name="error">Standard error: messages and progress.</param>
    /// <param name="progressThreshold">The file size in bytes from which progress is written.</param>
    /// <returns>The exit code.</returns>
    public static int Run(IReadOnlyList<string> args, TextWriter output, TextWriter error, long progressThreshold = DefaultProgressThreshold)
        => throw new NotImplementedException();
}
