namespace CpmUpdateTool;

public interface IConsole
{
    void WriteLine(string message);
    void Write(string message);

    async Task WriteBusyAsync(CancellationToken token)
    {
        try
        {
            await Task.Delay(-1, token);
        }
        catch (TaskCanceledException) { }
    }

    void ErrorWriteLine(string message);
    string? ReadLine();
}
