using Sl23.Reader.Cli;

namespace Sl23.Reader.Tests;

public class ProgramTests
{
    [Fact]
    public void NoArgumentsIsAUsageError()
    {
        using var output = new StringWriter();

        var code = Program.Run([], output);

        Assert.Equal(2, code);
        Assert.Equal(string.Empty, output.ToString());
    }

    [Fact]
    public void AnUnknownCommandIsAUsageError()
    {
        using var output = new StringWriter();

        var code = Program.Run(["frobnicate"], output);

        Assert.Equal(2, code);
        Assert.Equal(string.Empty, output.ToString());
    }
}
