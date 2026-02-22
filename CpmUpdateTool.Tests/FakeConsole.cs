using System.Text;

namespace CpmUpdateTool.Tests;

internal class FakeConsole : IConsole
{
    private readonly Queue<string?> inputs = new();
    public readonly StringBuilder Out = new();
    public readonly StringBuilder Err = new();

    public void QueueInput(string line) => this.inputs.Enqueue(line);

    public string? ReadLine() =>
        this.inputs.Count > 0 ? this.inputs.Dequeue() : null;

    public void WriteLine(string message) => this.Out.AppendLine(message);

    public void Write(string message) => this.Out.Append(message);

    public void ErrorWriteLine(string message) => this.Err.AppendLine(message);
}
