using System.CommandLine;

internal static class HelpVerifier
{
    public static (
        int exitCode,
        string output,
        string errorOutput
    ) GetHelp<TCommand>(Command command)
    {
        // ARRANGE
        var args = new[] { "--help" };

        // ACT
        var pr = command.Parse(args);
        using var outputWriter = new StringWriter();
        using var errorWriter = new StringWriter();
        pr.InvocationConfiguration.Output = outputWriter;
        pr.InvocationConfiguration.Error = errorWriter;
        int exitCode = pr.Invoke();
        var errorOutput = errorWriter.ToString();
        var stdOutput = outputWriter.ToString();
        return (exitCode, stdOutput, errorOutput);
    }
}
