namespace CpmUpdateTool;

public interface IConsole
{
    void WriteLine(string message);
    void Write(string message);

    async Task WriteBusyAsync(CancellationToken token)
    {
        await Task.Delay(-1, token);
    }

    void ErrorWriteLine(string message);
    string? ReadLine();
}

internal class RealConsole : IConsole
{
    public void WriteLine(string message) => Console.WriteLine(message);

    public void Write(string message) => Console.Write(message);

    public void ErrorWriteLine(string message) =>
        Console.Error.WriteLine(message);

    public string? ReadLine() => Console.ReadLine();

    public async Task WriteBusyAsync(CancellationToken token)
    {
        char[] spinner = ['|', '/', '-', '\\'];
        int idx = 0;
        while (!token.IsCancellationRequested)
        {
            Console.Write($"\r{spinner[idx]}");
            idx = (idx + 1) % spinner.Length;
            try
            {
                await Task.Delay(100, token);
            }
            catch (TaskCanceledException) { }
        }

        Console.Write("\r");
    }
}
