namespace CpmUpdateTool;

public interface IFileSystem
{
    bool Exists(string path);
    Stream OpenRead(string path);
    Stream OpenWrite(string path);
    string CombinePaths(string path, string path1);
}
