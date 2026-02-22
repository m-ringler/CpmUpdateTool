namespace CpmUpdateTool;

public interface IFileSystem
{
    bool Exists(string path);
    Stream OpenRead(string path);
    Stream OpenWrite(string path);

    string CombinePaths(string path, string path1);
}

internal class RealFileSystem : IFileSystem
{
    public bool Exists(string path) => File.Exists(path);

    public Stream OpenRead(string path) => File.OpenRead(path);

    public Stream OpenWrite(string path) => File.Open(path, FileMode.Create, FileAccess.Write);

    public string CombinePaths(string path, string path1) =>
        Path.Combine(path, path1);
}
