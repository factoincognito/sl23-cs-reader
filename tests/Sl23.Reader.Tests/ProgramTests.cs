using Sl23.Reader.Cli;

namespace Sl23.Reader.Tests;

public class ProgramTests
{
    [Fact]
    public void NoArgumentsIsAUsageError()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();

        var code = Program.Run([], output, error);

        Assert.Equal(2, code);
        Assert.Equal(string.Empty, output.ToString());
        Assert.Contains("No command given.", error.ToString(), StringComparison.Ordinal);
        Assert.Contains("Usage:", error.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void AnUnknownCommandIsAUsageErrorThatNamesTheCommand()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();

        var code = Program.Run(["frobnicate"], output, error);

        Assert.Equal(2, code);
        Assert.Equal(string.Empty, output.ToString());
        Assert.Contains("Unknown command: frobnicate", error.ToString(), StringComparison.Ordinal);
        Assert.Contains("Usage:", error.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void ARunWithoutOutputWritersIsRefused()
    {
        using var writer = new StringWriter();

        Assert.Throws<ArgumentNullException>(() => Program.Run(null!, writer, writer));
        Assert.Throws<ArgumentNullException>(() => Program.Run([], null!, writer));
        Assert.Throws<ArgumentNullException>(() => Program.Run([], writer, null!));
    }
}
