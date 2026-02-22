using System.Text;

namespace CpmUpdateTool.Tests;

internal class FakeFileSystem : IFileSystem
{
    private readonly Dictionary<string, string> existing = new(
        StringComparer.OrdinalIgnoreCase
    );

    public void AddFile(string path, string content = "") =>
        this.existing[path] = content;

    public string CombinePaths(string path, string path1)
    {
        return path[^1] == '/' ? path + path1 : path + "/" + path1;
    }

    public string this[string path] => this.existing[path];

    public bool Exists(string path) => this.existing.ContainsKey(path);

    public Stream OpenRead(string path)
    {
        if (!this.existing.TryGetValue(path, out var content))
            throw new FileNotFoundException($"File not found: {path}");

        return new MemoryStream(Encoding.UTF8.GetBytes(content));
    }

    public Stream OpenWrite(string path)
    {
        var ms = new DisposableStream(self =>
        {
            var bytes = self.ToArray();

            var updatedContent = Encoding.UTF8.GetString(bytes);

            // Remove BOM if present
            if (updatedContent.Length != 0 && updatedContent[0] == 65279)
            {
                updatedContent = updatedContent[1..];
            }

            this.existing[path] = updatedContent;
        });

        return ms;
    }

    private sealed class DisposableStream(Action<DisposableStream> onDispose)
        : MemoryStream()
    {
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                onDispose?.Invoke(this);
            }
        }
    }
}
