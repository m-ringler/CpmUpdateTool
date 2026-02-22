namespace CpmUpdateTool;

public interface ICmdRunner
{
    Task<string> ListOutdatedAsync(bool includePrerelease);
    Task<int> UpdatePackageAsync(string packageId, string version);
}
